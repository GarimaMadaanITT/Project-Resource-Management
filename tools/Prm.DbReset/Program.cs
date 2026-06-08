using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prm.Infrastructure.Persistence;
using Prm.Infrastructure.Persistence.Seeding;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((_, config) =>
    {
        config.AddUserSecrets(typeof(Program).Assembly, optional: true);
    })
    .ConfigureServices((context, services) =>
    {
        var connectionString = context.Configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Connection string 'Default' is not configured. Set it via User Secrets on src/Prm.Api or tools/Prm.DbReset.");

        services.AddDbContext<PrmDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(PrmDbContext).Assembly.FullName);
                npgsql.EnableRetryOnFailure(maxRetryCount: 3);
            }));

        services.AddLogging(builder => builder.AddConsole());
        services.AddScoped<DataSeeder>();
    })
    .Build();

using var scope = host.Services.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<PrmDbContext>();
await context.Database.MigrateAsync();

var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
await seeder.ClearAndReseedAsync();

Console.WriteLine("Database cleared and reseeded successfully.");
