using System.Net;
using System.Net.Http.Json;

namespace Prm.Tests.Integration;

[Collection(nameof(IntegrationTestCollection))]
public class ManagerProjectsIntegrationTests : IAsyncLifetime
{
    private readonly PrmWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ManagerProjectsIntegrationTests(PrmWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        var token = await _factory.LoginAsManagerAsync(_client);
        PrmWebApplicationFactory.Authorize(_client, token);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetProjects_Returns_Only_Owned_Projects()
    {
        var projects = await _client.GetFromJsonAsync<List<ProjectItem>>("/api/manager/projects");

        Assert.NotNull(projects);
        Assert.Equal(2, projects!.Count);
        Assert.Contains(projects, p => p.Name == "Alpha Portal");
        Assert.Contains(projects, p => p.Name == "Beta CRM");
        Assert.DoesNotContain(projects, p => p.Name == "Gamma Rewrite");
    }

    [Fact]
    public async Task GetProjectDetail_Returns_Milestones_And_Risk_Flags()
    {
        var projects = await _client.GetFromJsonAsync<List<ProjectItem>>("/api/manager/projects");
        var alphaId = projects!.First(p => p.Name == "Alpha Portal").Id;

        var detail = await _client.GetFromJsonAsync<ProjectDetailResponse>($"/api/manager/projects/{alphaId}");

        Assert.NotNull(detail);
        Assert.Equal("AtRisk", detail!.HealthStatus);
        Assert.NotEmpty(detail.Milestones);
        Assert.NotEmpty(detail.RiskFlags);
        Assert.NotEmpty(detail.Allocations);
    }

    [Fact]
    public async Task GetProjectDetail_Returns_403_For_Other_Manager_Project()
    {
        var nehaClient = _factory.CreateClient();
        var nehaToken = await _factory.LoginAsync(nehaClient, "neha.joshi", "Manager@1234");
        PrmWebApplicationFactory.Authorize(nehaClient, nehaToken);

        var ankitProjects = await _client.GetFromJsonAsync<List<ProjectItem>>("/api/manager/projects");
        var alphaId = ankitProjects!.First(p => p.Name == "Alpha Portal").Id;

        var response = await nehaClient.GetAsync($"/api/manager/projects/{alphaId}");
        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.Forbidden);
    }

    private sealed record ProjectItem(int Id, string Name, DateOnly EndDate, string HealthStatus);

    private sealed record ProjectDetailResponse(
        int Id,
        string Name,
        string Description,
        DateOnly EndDate,
        string HealthStatus,
        List<RiskFlag> RiskFlags,
        List<MilestoneItem> Milestones,
        List<AllocationItem> Allocations);

    private sealed record RiskFlag(string Code, string Message, bool IsRisk);
    private sealed record MilestoneItem(int Id, string Title, DateOnly DueDate, string Status, bool IsOverdue);
    private sealed record AllocationItem(string EmployeeName, int UtilisationPercent, DateOnly FromDate, DateOnly ToDate);
}
