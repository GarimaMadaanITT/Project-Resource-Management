using System.Net;
using System.Net.Http.Json;

namespace Prm.Tests.Integration;

[Collection(nameof(IntegrationTestCollection))]
public class ManagerDashboardIntegrationTests : IAsyncLifetime
{
    private readonly PrmWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ManagerDashboardIntegrationTests(PrmWebApplicationFactory factory)
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
    public async Task GetDashboard_Returns_Team_Groups()
    {
        var dashboard = await _client.GetFromJsonAsync<DashboardResponse>("/api/manager/dashboard");

        Assert.NotNull(dashboard);
        Assert.Contains(dashboard!.Bench, e => e.Name == "Anil Mehta");
        Assert.Contains(dashboard.Full, e => e.Name == "Ravi Kumar");
        Assert.Contains(dashboard.Partial, e => e.Name == "Dev Patel");
        Assert.DoesNotContain(dashboard.Bench.Concat(dashboard.Partial).Concat(dashboard.Full), e => e.Name == "Priya Sharma");
    }

    [Fact]
    public async Task GetEmployeeDetail_Returns_Ravi_Allocations()
    {
        var dashboard = await _client.GetFromJsonAsync<DashboardResponse>("/api/manager/dashboard");
        var raviId = dashboard!.Full.First(e => e.Name == "Ravi Kumar").Id;

        var detail = await _client.GetFromJsonAsync<EmployeeDetailResponse>(
            $"/api/manager/dashboard/employees/{raviId}");

        Assert.NotNull(detail);
        Assert.Equal(2, detail!.ActiveAllocations.Count);
        Assert.NotEmpty(detail.RecentActivityTags);
    }

    private sealed record DashboardResponse(
        List<DashboardEmployee> Bench,
        List<DashboardEmployee> Partial,
        List<DashboardEmployee> Full,
        int BenchCount,
        int PartialCount,
        int FullCount);

    private sealed record DashboardEmployee(
        int Id,
        string Name,
        string Department,
        int UtilisationPercent,
        int AvailabilityPercent,
        string SkillsSummary);

    private sealed record EmployeeDetailResponse(
        int Id,
        string Name,
        string Department,
        string Status,
        int UtilisationPercent,
        List<string> ProfileSkills,
        List<AllocationItem> ActiveAllocations,
        List<string> RecentActivityTags);

    private sealed record AllocationItem(
        string ProjectName,
        int UtilisationPercent,
        DateOnly FromDate,
        DateOnly ToDate);
}
