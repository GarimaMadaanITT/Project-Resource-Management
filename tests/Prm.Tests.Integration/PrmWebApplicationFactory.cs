using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Prm.Infrastructure.Persistence;
using Prm.Infrastructure.Persistence.Seeding;

namespace Prm.Tests.Integration;

public class PrmWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("ConnectionStrings:Default", "DataSource=prm_integration_test;Mode=Memory;Cache=Shared");
        builder.UseSetting("Jwt:Key", "IntegrationTestSigningKeyAtLeast32CharsLong!");
        builder.UseSetting("Jwt:Issuer", "PrmApi");
        builder.UseSetting("Jwt:Audience", "PrmClient");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "DataSource=prm_integration_test;Mode=Memory;Cache=Shared",
                ["Jwt:Key"] = "IntegrationTestSigningKeyAtLeast32CharsLong!",
                ["Jwt:Issuer"] = "PrmApi",
                ["Jwt:Audience"] = "PrmClient"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<PrmDbContext>));
            services.RemoveAll(typeof(PrmDbContext));

            _connection = new SqliteConnection("DataSource=prm_integration_test;Mode=Memory;Cache=Shared");
            _connection.Open();

            services.AddDbContext<PrmDbContext>(options =>
                options.UseSqlite(_connection));
        });
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrmDbContext>();
        await context.Database.EnsureCreatedAsync();

        var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
        await seeder.SeedAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
        await seeder.ClearAndReseedAsync();
    }

    public new async Task DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        await base.DisposeAsync();
    }

    public async Task<string> LoginAsAdminAsync(HttpClient client) =>
        await LoginAndChangePasswordAsync(client, "admin", "Admin@1234", "Admin@5678");

    public async Task<string> LoginAsManagerAsync(HttpClient client) =>
        await LoginAsync(client, "ankit.shah", "Manager@1234");

    public async Task<string> LoginAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { username, password });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("token").GetString()!;
    }

    public async Task<string> LoginAndChangePasswordAsync(
        HttpClient client,
        string username,
        string password,
        string newPassword)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { username, password });
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<JsonElement>();
        var token = login.GetProperty("token").GetString()!;
        Authorize(client, token);

        if (login.GetProperty("forcePasswordChange").GetBoolean())
        {
            var changeResponse = await client.PostAsJsonAsync("/api/auth/change-password", new
            {
                newPassword,
                confirmPassword = newPassword
            });
            changeResponse.EnsureSuccessStatusCode();
            var change = await changeResponse.Content.ReadFromJsonAsync<JsonElement>();
            token = change.GetProperty("token").GetString()!;
            Authorize(client, token);
        }

        return token;
    }

    public static void Authorize(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}

public class PrmIntegrationTestBase : IClassFixture<PrmWebApplicationFactory>, IAsyncLifetime
{
    protected readonly PrmWebApplicationFactory Factory;
    protected readonly HttpClient Client;
    protected string AdminToken = string.Empty;

    protected PrmIntegrationTestBase(PrmWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await Factory.ResetDatabaseAsync();
        AdminToken = await Factory.LoginAsAdminAsync(Client);
        PrmWebApplicationFactory.Authorize(Client, AdminToken);
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
