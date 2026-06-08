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
