using System.Net;
using System.Net.Http.Json;

namespace Prm.Tests.Integration;

[Collection(nameof(IntegrationTestCollection))]
public class HealthAndDatabaseIntegrationTests : PrmIntegrationTestBase
{
    public HealthAndDatabaseIntegrationTests(PrmWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Health_Returns_200()
    {
        var client = Factory.CreateClient();
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DatabaseStatus_Returns_Connected_With_Seed_Counts()
    {
        var client = Factory.CreateClient();
        var response = await client.GetFromJsonAsync<DatabaseStatusResponse>("/api/database/status");

        Assert.NotNull(response);
        Assert.True(response!.Connected);
        Assert.True(response.Users >= 9);
        Assert.True(response.Employees >= 5);
        Assert.True(response.Projects >= 4);
        Assert.True(response.BootstrapAdmin);
    }

    private sealed record DatabaseStatusResponse(
        bool Connected,
        int Users,
        int Employees,
        int Projects,
        bool BootstrapAdmin);
}
