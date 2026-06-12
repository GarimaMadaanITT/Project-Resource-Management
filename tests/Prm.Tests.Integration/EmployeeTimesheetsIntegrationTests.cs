using System.Net;
using System.Net.Http.Json;
using Prm.Application.Validation;

namespace Prm.Tests.Integration;

[Collection(nameof(IntegrationTestCollection))]
public class EmployeeTimesheetsIntegrationTests : IAsyncLifetime
{
    private readonly PrmWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private int _betaCrmProjectId;

    public EmployeeTimesheetsIntegrationTests(PrmWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        var token = await _factory.LoginAsEmployeeAsync(_client);
        PrmWebApplicationFactory.Authorize(_client, token);

        var adminClient = _factory.CreateClient();
        var adminToken = await _factory.LoginAsAdminAsync(adminClient);
        PrmWebApplicationFactory.Authorize(adminClient, adminToken);

        var projects = await adminClient.GetFromJsonAsync<List<ProjectItem>>("/api/admin/projects");
        _betaCrmProjectId = projects!.First(project => project.Name == "Beta CRM").Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetActivityTags_Returns_Predefined_List()
    {
        var response = await _client.GetFromJsonAsync<ActivityTagsResponse>("/api/employee/timesheets/activity-tags");

        Assert.NotNull(response);
        Assert.True(response!.AllowsCustomOther);
        Assert.Contains("Bug Fixing", response.PredefinedTags);
    }

    [Fact]
    public async Task SubmitTimesheet_Succeeds_For_Dev_On_New_Week()
    {
        var response = await _client.PostAsJsonAsync("/api/employee/timesheets", new
        {
            weekStart = "2026-05-11",
            entries = new[]
            {
                new
                {
                    projectId = _betaCrmProjectId,
                    hours = 18,
                    activityTags = new[] { "Backend API Development", "Bug Fixing" }
                }
            }
        });

        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<SubmitResponse>();
        Assert.Equal(18, body!.TotalHours);
        Assert.Equal("Submitted", body.Status);
    }

    [Fact]
    public async Task SubmitTimesheet_Returns_409_When_Week_Already_Submitted()
    {
        var raviClient = _factory.CreateClient();
        var token = await _factory.LoginAsync(raviClient, "ravi.kumar", "Employee@1234");
        PrmWebApplicationFactory.Authorize(raviClient, token);

        var response = await raviClient.PostAsJsonAsync("/api/employee/timesheets", new
        {
            weekStart = "2026-05-04",
            entries = new[]
            {
                new
                {
                    projectId = _betaCrmProjectId,
                    hours = 10,
                    activityTags = new[] { "Bug Fixing" }
                }
            }
        });

        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task SubmitTimesheet_Returns_400_When_Hours_Exceed_Project_Cap()
    {
        var response = await _client.PostAsJsonAsync("/api/employee/timesheets", new
        {
            weekStart = "2026-05-18",
            entries = new[]
            {
                new
                {
                    projectId = _betaCrmProjectId,
                    hours = 25,
                    activityTags = new[] { "Bug Fixing" }
                }
            }
        });

        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SubmitTimesheet_Returns_400_When_No_Allocations()
    {
        var anilClient = _factory.CreateClient();
        var token = await _factory.LoginAsync(anilClient, "anil.mehta", "Employee@1234");
        PrmWebApplicationFactory.Authorize(anilClient, token);

        var response = await anilClient.PostAsJsonAsync("/api/employee/timesheets", new
        {
            weekStart = "2026-05-11",
            entries = new[]
            {
                new
                {
                    projectId = _betaCrmProjectId,
                    hours = 10,
                    activityTags = new[] { "Bug Fixing" }
                }
            }
        });

        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetMyTimesheets_Includes_Missed_And_Submitted_Weeks()
    {
        await _client.PostAsJsonAsync("/api/employee/timesheets", new
        {
            weekStart = "2026-05-11",
            entries = new[]
            {
                new
                {
                    projectId = _betaCrmProjectId,
                    hours = 18,
                    activityTags = new[] { "Bug Fixing" }
                }
            }
        });

        var history = await _client.GetFromJsonAsync<MyTimesheetsResponse>("/api/employee/timesheets");

        Assert.NotNull(history);
        Assert.Contains(history!.Weeks, week =>
            week.WeekStart == new DateOnly(2026, 5, 4) && week.Status == "Missed");
        Assert.Contains(history.Weeks, week =>
            week.WeekStart == new DateOnly(2026, 5, 11) && week.Status == "Submitted" && week.TotalHours == 18);
    }

    [Fact]
    public async Task GetWeekDetail_Returns_Entries_For_Submitted_Week()
    {
        await _client.PostAsJsonAsync("/api/employee/timesheets", new
        {
            weekStart = "2026-05-11",
            entries = new[]
            {
                new
                {
                    projectId = _betaCrmProjectId,
                    hours = 18,
                    activityTags = new[] { "Backend API Development", "Bug Fixing" }
                }
            }
        });

        var detail = await _client.GetFromJsonAsync<WeekDetailResponse>("/api/employee/timesheets/2026-05-11");

        Assert.NotNull(detail);
        Assert.Equal("Submitted", detail!.Status);
        Assert.Single(detail.Entries);
        Assert.Equal(18, detail.Entries[0].Hours);
    }

    private sealed record ProjectItem(int Id, string Name, string ManagerName, string EndDate, string Status, int StoryPointsDone, int TotalStoryPoints);
    private sealed record ActivityTagsResponse(List<string> PredefinedTags, bool AllowsCustomOther);
    private sealed record SubmitResponse(DateOnly WeekStart, decimal TotalHours, string Status, string Message);
    private sealed record MyTimesheetsResponse(List<WeekItem> Weeks);
    private sealed record WeekItem(DateOnly WeekStart, decimal TotalHours, string Status);
    private sealed record WeekDetailResponse(DateOnly WeekStart, decimal TotalHours, string Status, List<EntryItem> Entries);
    private sealed record EntryItem(string ProjectName, decimal Hours, List<string> ActivityTags);
}

[Collection(nameof(IntegrationTestCollection))]
public class EmployeeAuthorizationIntegrationTests
{
    private readonly PrmWebApplicationFactory _factory;

    public EmployeeAuthorizationIntegrationTests(PrmWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Employee_Endpoints_Require_Authentication()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/employee/timesheets");
        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Manager_Cannot_Access_Employee_Timesheets()
    {
        var client = _factory.CreateClient();
        var token = await _factory.LoginAsManagerAsync(client);
        PrmWebApplicationFactory.Authorize(client, token);

        var response = await client.GetAsync("/api/employee/timesheets");
        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.Forbidden);
    }
}
