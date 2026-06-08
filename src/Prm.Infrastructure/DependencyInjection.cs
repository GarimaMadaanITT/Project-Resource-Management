using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Prm.Application.Common;
using Prm.Application.Interfaces;
using Prm.Infrastructure.Auth;
using Prm.Infrastructure.Persistence;
using Prm.Infrastructure.Persistence.Seeding;
using Prm.Infrastructure.Repositories;
using Prm.Infrastructure.Security;
using Prm.Infrastructure.Services;

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
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IAllocationRepository, AllocationRepository>();
        services.AddScoped<ISystemSettingsRepository, SystemSettingsRepository>();
        services.AddScoped<ISkillRepository, SkillRepository>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IDatabaseHealthService, DatabaseHealthService>();

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
