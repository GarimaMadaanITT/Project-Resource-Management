using Microsoft.EntityFrameworkCore;
using Prm.Application;
using Prm.Api;
using Prm.Infrastructure;
using Prm.Infrastructure.Persistence;
using Prm.Infrastructure.Persistence.Seeding;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddPrmSwagger();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks().AddDbContextCheck<PrmDbContext>();

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddHostedService<Prm.Infrastructure.Scheduling.PrmSchedulerHostedService>();
}

var app = builder.Build();

if (args.Contains("--reset-database", StringComparer.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<PrmDbContext>();
    await context.Database.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await seeder.ClearAndReseedAsync();

    Console.WriteLine("Database cleared and reseeded successfully.");
    return;
}

if (args.Contains("--seed-notification-test-data", StringComparer.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<PrmDbContext>();
    await context.Database.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    var result = await seeder.SeedNotificationTestDataAsync();

    Console.WriteLine("Notification test data seeded successfully.");
    Console.WriteLine(result.Message);
    foreach (var employee in result.PreparedEmployees)
    {
        Console.WriteLine($"  - {employee}");
    }
    return;
}

if (args.Contains("--run-scheduler-now", StringComparer.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<PrmDbContext>();
    await context.Database.MigrateAsync();

    var scheduler = scope.ServiceProvider.GetRequiredService<Prm.Application.Interfaces.ISchedulerOrchestrator>();
    await scheduler.RunAsync();

    Console.WriteLine("Scheduler completed. Check API logs and Mailtrap for notification emails.");
    return;
}

var forceComplianceIndex = Array.FindIndex(
    args,
    argument => argument.Equals("--force-timesheet-compliance", StringComparison.OrdinalIgnoreCase));
if (forceComplianceIndex >= 0 && forceComplianceIndex + 1 < args.Length)
{
    var username = args[forceComplianceIndex + 1];

    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<PrmDbContext>();
    await context.Database.MigrateAsync();

    var complianceService = scope.ServiceProvider
        .GetRequiredService<Prm.Application.Interfaces.ITimesheetMissedDetectionService>();
    var result = await complianceService.ForceAdvanceComplianceAsync(username);

    Console.WriteLine($"Timesheet compliance forced for {result.Username}.");
    Console.WriteLine(result.Message);
    Console.WriteLine($"Frozen: {result.TimesheetSubmissionFrozen}");
    return;
}

if (!app.Environment.IsEnvironment("Testing"))
{
    await app.Services.ApplyMigrationsAndSeedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<Prm.Api.Middleware.GlobalExceptionMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseMiddleware<Prm.Api.Middleware.ForcePasswordChangeMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
