using System.Net;
using System.Net.Http.Json;

namespace Prm.Tests.Integration;

[Collection(nameof(IntegrationTestCollection))]
public class ManagerTimesheetsIntegrationTests : IAsyncLifetime
{
    private readonly PrmWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ManagerTimesheetsIntegrationTests(PrmWebApplicationFactory factory)
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
    public async Task GetTeamTimesheets_Returns_Ravi_Entries_For_Seeded_Week()
    {
        var response = await _client.GetFromJsonAsync<TimesheetsResponse>(
            "/api/manager/timesheets?weekStart=2026-05-04");

        Assert.NotNull(response);
        Assert.Equal(new DateOnly(2026, 5, 4), response!.WeekStart);
        Assert.Contains(response.Rows, row =>
            row.EmployeeName == "Ravi Kumar"
            && row.ProjectName == "Alpha Portal"
            && row.Hours == 20
            && row.Status == "Submitted");
    }

    [Fact]
    public async Task GetEmployeeTimesheetDetail_Returns_Ravi_Entries_With_Activity_Tags()
    {
        var dashboard = await _client.GetFromJsonAsync<DashboardResponse>("/api/manager/dashboard");
        var raviId = dashboard!.Full.First(e => e.Name == "Ravi Kumar").Id;

        var detail = await _client.GetFromJsonAsync<EmployeeTimesheetDetailResponse>(
            $"/api/manager/timesheets/employees/{raviId}?weekStart=2026-05-04");

        Assert.NotNull(detail);
        Assert.Equal("Ravi Kumar", detail!.EmployeeName);
        Assert.Equal("Submitted", detail.Status);
        Assert.Contains(detail.Entries, entry =>
            entry.ProjectName == "Alpha Portal"
            && entry.Hours == 20
            && entry.ActivityTags.Contains("Microservices"));
    }

    [Fact]
    public async Task GetEmployeeTimesheetDetail_Returns_Missed_When_Not_Submitted()
    {
        var dashboard = await _client.GetFromJsonAsync<DashboardResponse>("/api/manager/dashboard");
        var devId = dashboard!.Partial.First(e => e.Name == "Dev Patel").Id;

        var detail = await _client.GetFromJsonAsync<EmployeeTimesheetDetailResponse>(
            $"/api/manager/timesheets/employees/{devId}?weekStart=2026-05-04");

        Assert.NotNull(detail);
        Assert.Equal("Missed", detail!.Status);
        Assert.Contains(detail.Entries, entry =>
            entry.ProjectName == "Beta CRM" && entry.Hours == 0 && entry.ActivityTags.Count == 0);
    }

    [Fact]
    public async Task GetEmployeeTimesheetDetail_Returns_404_For_Employee_Not_On_Team()
    {
        var dashboard = await _client.GetFromJsonAsync<DashboardResponse>("/api/manager/dashboard");
        var raviId = dashboard!.Full.First(e => e.Name == "Ravi Kumar").Id;

        var nehaClient = _factory.CreateClient();
        var token = await _factory.LoginAsync(nehaClient, "neha.joshi", "Manager@1234");
        PrmWebApplicationFactory.Authorize(nehaClient, token);

        var response = await nehaClient.GetAsync(
            $"/api/manager/timesheets/employees/{raviId}?weekStart=2026-05-04");

        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.NotFound);
    }

    private sealed record TimesheetsResponse(DateOnly WeekStart, List<TimesheetRow> Rows);
    private sealed record TimesheetRow(string EmployeeName, string ProjectName, decimal Hours, string Status);
    private sealed record DashboardResponse(
        List<DashboardEmployee> Bench,
        List<DashboardEmployee> Partial,
        List<DashboardEmployee> Full,
        int BenchCount,
        int PartialCount,
        int FullCount);
    private sealed record DashboardEmployee(int Id, string Name, string Department, int UtilisationPercent, int AvailabilityPercent, string SkillsSummary);
    private sealed record EmployeeTimesheetDetailResponse(
        int EmployeeId,
        string EmployeeName,
        DateOnly WeekStart,
        string Status,
        List<TimesheetEntryDetail> Entries);
    private sealed record TimesheetEntryDetail(string ProjectName, decimal Hours, List<string> ActivityTags);
}

[Collection(nameof(IntegrationTestCollection))]
public class ManagerAuthorizationIntegrationTests
{
    private readonly PrmWebApplicationFactory _factory;

    public ManagerAuthorizationIntegrationTests(PrmWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Manager_Endpoints_Require_Authentication()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/manager/dashboard");
        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Employee_Cannot_Access_Manager_Dashboard()
    {
        var client = _factory.CreateClient();
        var token = await _factory.LoginAsync(client, "ravi.kumar", "Employee@1234");
        PrmWebApplicationFactory.Authorize(client, token);

        var response = await client.GetAsync("/api/manager/dashboard");
        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.Forbidden);
    }
}
