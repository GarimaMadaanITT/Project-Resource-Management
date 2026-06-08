using System.Net;

namespace Prm.Tests.Integration;

[Collection(nameof(IntegrationTestCollection))]
public class AdminAuthorizationIntegrationTests
{
    private readonly PrmWebApplicationFactory _factory;

    public AdminAuthorizationIntegrationTests(PrmWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Admin_Endpoints_Require_Authentication()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/admin/users");
        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Manager_Cannot_Access_Admin_Users()
    {
        var client = _factory.CreateClient();
        var token = await _factory.LoginAsync(client, "ankit.shah", "Manager@1234");
        PrmWebApplicationFactory.Authorize(client, token);

        var response = await client.GetAsync("/api/admin/users");
        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Employee_Cannot_Access_Admin_Projects()
    {
        var client = _factory.CreateClient();
        var token = await _factory.LoginAsync(client, "ravi.kumar", "Employee@1234");
        PrmWebApplicationFactory.Authorize(client, token);

        var response = await client.GetAsync("/api/admin/projects");
        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.Forbidden);
    }
}
