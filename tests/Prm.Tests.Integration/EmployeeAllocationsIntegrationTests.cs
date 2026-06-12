using System.Net.Http.Json;

namespace Prm.Tests.Integration;

[Collection(nameof(IntegrationTestCollection))]
public class EmployeeAllocationsIntegrationTests : IAsyncLifetime
{
    private readonly PrmWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EmployeeAllocationsIntegrationTests(PrmWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        var token = await _factory.LoginAsEmployeeAsync(_client);
        PrmWebApplicationFactory.Authorize(_client, token);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetMyAllocations_Returns_Dev_Beta_Crm_Allocation()
    {
        var adminClient = _factory.CreateClient();
        var adminToken = await _factory.LoginAsAdminAsync(adminClient);
        PrmWebApplicationFactory.Authorize(adminClient, adminToken);

        var projects = await adminClient.GetFromJsonAsync<List<ProjectItem>>("/api/admin/projects");
        var betaCrmProjectId = projects!.First(project => project.Name == "Beta CRM").Id;

        var response = await _client.GetFromJsonAsync<MyAllocationsResponse>("/api/employee/allocations");

        Assert.NotNull(response);
        Assert.Equal(50, response!.TotalUtilisationPercent);
        Assert.Single(response.Allocations);
        Assert.Equal(betaCrmProjectId, response.Allocations[0].ProjectId);
        Assert.Equal("Beta CRM", response.Allocations[0].ProjectName);
        Assert.Equal("Active", response.Allocations[0].Status);
    }

    private sealed record ProjectItem(int Id, string Name, string ManagerName, string EndDate, string Status, int StoryPointsDone, int TotalStoryPoints);
    private sealed record MyAllocationsResponse(List<AllocationItem> Allocations, int TotalUtilisationPercent);
    private sealed record AllocationItem(
        int ProjectId,
        string ProjectName,
        int UtilisationPercent,
        DateOnly FromDate,
        DateOnly ToDate,
        string Status);
}
