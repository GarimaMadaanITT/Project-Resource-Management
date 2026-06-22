using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Prm.Application.Common;
using Prm.Application.Interfaces;
using Prm.Infrastructure.Auth;
using Prm.Infrastructure.Persistence;
using Prm.Infrastructure.Persistence.Seeding;
using Prm.Infrastructure.Repositories;
using Prm.Infrastructure.Security;
using Prm.Infrastructure.Services;
using Prm.Infrastructure.Ai;
using Prm.Infrastructure.Email;

namespace Prm.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Connection string 'Default' is not configured. Set it via User Secrets.");

        services.AddDbContext<PrmDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(PrmDbContext).Assembly.FullName);
                npgsql.EnableRetryOnFailure(maxRetryCount: 3);
            }));

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddScoped<DataSeeder>();
        services.AddScoped<NotificationTestDataSeeder>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IResourceProfileRepository, ResourceProfileRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IAllocationRepository, AllocationRepository>();
        services.AddScoped<ITimesheetRepository, TimesheetRepository>();
        services.AddScoped<ISystemSettingsRepository, SystemSettingsRepository>();
        services.AddScoped<ISkillRepository, SkillRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IDatabaseHealthService, DatabaseHealthService>();

        services.AddHttpClient("Gemini", client =>
        {
            client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        services.AddHttpClient("Groq", client =>
        {
            client.BaseAddress = new Uri("https://api.groq.com/");
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        services.AddOptions<OllamaOptions>()
            .Bind(configuration.GetSection(OllamaOptions.SectionName))
            .PostConfigure(OllamaOptions.Normalize)
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Model),
                "Ollama:Model must be configured in appsettings.json.")
            .ValidateOnStart();
        services.AddHttpClient("Ollama", (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds <= 0 ? 120 : options.TimeoutSeconds);
        });
        services.AddScoped<ILlmProvider, GeminiLlmProvider>();
        services.AddScoped<ILlmProvider, GroqLlmProvider>();
        services.AddScoped<ILlmProvider, OllamaLlmProvider>();
        services.AddScoped<ILlmProviderRegistry, LlmProviderRegistry>();
        services.AddScoped<DeterministicLlmProvider>();
        services.AddScoped<ILlmCompletionService, LlmCompletionService>();

        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.AddScoped<LoggingEmailService>();
        services.AddScoped<SmtpEmailService>();
        services.AddScoped<IEmailService, CompositeEmailService>();
        services.AddScoped<ITimesheetComplianceRepository, TimesheetComplianceRepository>();
        services.AddScoped<INotificationLogRepository, NotificationLogRepository>();

        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? new JwtSettings();

        if (string.IsNullOrWhiteSpace(jwtSettings.Key) || jwtSettings.Key.Length < ValidationConstants.MinJwtKeyLength)
        {
            var env = configuration["ASPNETCORE_ENVIRONMENT"];
            if (string.Equals(env, "Testing", StringComparison.OrdinalIgnoreCase))
            {
                jwtSettings.Key = "IntegrationTestSigningKeyAtLeast32CharsLong!";
                jwtSettings.Issuer ??= "PrmApi";
                jwtSettings.Audience ??= "PrmClient";
            }
            else
            {
                throw new InvalidOperationException("Jwt:Key must be at least 32 characters. Set it via User Secrets.");
            }
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthConstants.PolicyNames.AdminOnly, policy => policy.RequireRole(AuthConstants.RoleName(Domain.Enums.UserRole.Admin)))
            .AddPolicy(AuthConstants.PolicyNames.ManagerOnly, policy => policy.RequireRole(AuthConstants.RoleName(Domain.Enums.UserRole.Manager)))
            .AddPolicy(AuthConstants.PolicyNames.EmployeeOnly, policy => policy.RequireRole(AuthConstants.RoleName(Domain.Enums.UserRole.Employee)));

        return services;
    }

    public static async Task ApplyMigrationsAndSeedAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrmDbContext>();
        await context.Database.MigrateAsync(cancellationToken);

        var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
        await seeder.SeedAsync(cancellationToken);
    }
}
