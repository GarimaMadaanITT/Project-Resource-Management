using System.Net;
using System.Net.Http.Json;

namespace Prm.Tests.Integration;

[Collection(nameof(IntegrationTestCollection))]
public class ManagerAllocationsIntegrationTests : IAsyncLifetime
{
    private readonly PrmWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ManagerAllocationsIntegrationTests(PrmWebApplicationFactory factory)
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
    public async Task CreateAllocation_Succeeds_For_Bench_Employee()
    {
        var dashboard = await _client.GetFromJsonAsync<DashboardResponse>("/api/manager/dashboard");
        var anilId = dashboard!.Bench.First(e => e.Name == "Anil Mehta").Id;

        var projects = await _client.GetFromJsonAsync<List<ProjectItem>>("/api/manager/projects");
        var alphaId = projects!.First(p => p.Name == "Alpha Portal").Id;

        var response = await _client.PostAsJsonAsync("/api/manager/allocations", new
        {
            projectId = alphaId,
            employeeId = anilId,
            utilisationPercent = 50,
            fromDate = "2026-06-01",
            toDate = "2026-09-30"
        });

        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateAllocation_Returns_400_When_Over_100_Percent()
    {
        var dashboard = await _client.GetFromJsonAsync<DashboardResponse>("/api/manager/dashboard");
        var raviId = dashboard!.Full.First(e => e.Name == "Ravi Kumar").Id;

        var projects = await _client.GetFromJsonAsync<List<ProjectItem>>("/api/manager/projects");
        var alphaId = projects!.First(p => p.Name == "Alpha Portal").Id;

        var response = await _client.PostAsJsonAsync("/api/manager/allocations", new
        {
            projectId = alphaId,
            employeeId = raviId,
            utilisationPercent = 10,
            fromDate = "2026-03-01",
            toDate = "2026-06-30"
        });

        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task EndAllocation_Frees_Employee_When_No_Other_Active_Allocations()
    {
        var adminClient = _factory.CreateClient();
        var adminToken = await _factory.LoginAsAdminAsync(adminClient);
        PrmWebApplicationFactory.Authorize(adminClient, adminToken);

        var employees = await adminClient.GetFromJsonAsync<EmployeeListResponse>("/api/admin/employees");
        var anilId = employees!.Employees.First(e => e.Name == "Anil Mehta").Id;

        var projects = await _client.GetFromJsonAsync<List<ProjectItem>>("/api/manager/projects");
        var alphaId = projects!.First(p => p.Name == "Alpha Portal").Id;

        var createResponse = await _client.PostAsJsonAsync("/api/manager/allocations", new
        {
            projectId = alphaId,
            employeeId = anilId,
            utilisationPercent = 50,
            fromDate = "2026-01-01",
            toDate = "2026-12-31"
        });
        await PrmApiAssertions.AssertStatusAsync(createResponse, HttpStatusCode.Created);

        var allocation = await createResponse.Content.ReadFromJsonAsync<AllocationResponse>();

        var endResponse = await _client.PostAsJsonAsync($"/api/manager/allocations/{allocation!.Id}/end", new
        {
            endDate = "2026-06-07"
        });

        await PrmApiAssertions.AssertStatusAsync(endResponse, HttpStatusCode.OK);
        var endResult = await endResponse.Content.ReadFromJsonAsync<EndResponse>();
        Assert.Equal("Bench", endResult!.EmployeeStatus);
    }

    private sealed record DashboardResponse(
        List<DashboardEmployee> Bench,
        List<DashboardEmployee> Partial,
        List<DashboardEmployee> Full,
        int BenchCount,
        int PartialCount,
        int FullCount);

    private sealed record DashboardEmployee(int Id, string Name, string Department, int UtilisationPercent, int AvailabilityPercent, string SkillsSummary);
    private sealed record ProjectItem(int Id, string Name, DateOnly EndDate, string HealthStatus);
    private sealed record AllocationResponse(int Id, int ProjectId, string ProjectName, int EmployeeId, string EmployeeName, int UtilisationPercent, DateOnly FromDate, DateOnly ToDate);
    private sealed record EndResponse(string Message, string EmployeeStatus);
    private sealed record EmployeeListResponse(List<EmployeeItem> Employees, int Total, int AllocatedCount, int BenchCount);
    private sealed record EmployeeItem(int Id, string Name, string Department, string Status, bool IsActive, int UtilisationPercent);
}
