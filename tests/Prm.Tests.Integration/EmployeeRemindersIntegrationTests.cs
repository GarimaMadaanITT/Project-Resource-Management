using System.Net.Http.Json;
using Prm.Application.Validation;

namespace Prm.Tests.Integration;

[Collection(nameof(IntegrationTestCollection))]
public class EmployeeRemindersIntegrationTests : IAsyncLifetime
{
    private readonly PrmWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EmployeeRemindersIntegrationTests(PrmWebApplicationFactory factory)
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
    public async Task GetReminder_Returns_Missed_Timesheet_For_Previous_Week()
    {
        var response = await _client.GetFromJsonAsync<ReminderResponse>("/api/employee/reminders");

        Assert.NotNull(response);
        Assert.True(response!.HasReminder);
        Assert.Equal(ActiveDateHelper.GetPreviousCompletedWeekStart(), response.WeekStart);
        Assert.Contains("has not been submitted", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetReminder_Returns_No_Reminder_After_Previous_Week_Submitted()
    {
        var adminClient = _factory.CreateClient();
        var adminToken = await _factory.LoginAsAdminAsync(adminClient);
        PrmWebApplicationFactory.Authorize(adminClient, adminToken);

        var projects = await adminClient.GetFromJsonAsync<List<ProjectItem>>("/api/admin/projects");
        var betaCrmProjectId = projects!.First(project => project.Name == "Beta CRM").Id;
        var previousWeek = ActiveDateHelper.GetPreviousCompletedWeekStart();

        var submitResponse = await _client.PostAsJsonAsync("/api/employee/timesheets", new
        {
            weekStart = previousWeek.ToString("yyyy-MM-dd"),
            entries = new[]
            {
                new
                {
                    projectId = betaCrmProjectId,
                    hours = 10,
                    activityTags = new[] { "Bug Fixing" }
                }
            }
        });
        submitResponse.EnsureSuccessStatusCode();

        var response = await _client.GetFromJsonAsync<ReminderResponse>("/api/employee/reminders");

        Assert.NotNull(response);
        Assert.False(response!.HasReminder);
    }

    private sealed record ProjectItem(int Id, string Name, string ManagerName, string EndDate, string Status, int StoryPointsDone, int TotalStoryPoints);
    private sealed record ReminderResponse(bool HasReminder, DateOnly? WeekStart, string? Message);
}
