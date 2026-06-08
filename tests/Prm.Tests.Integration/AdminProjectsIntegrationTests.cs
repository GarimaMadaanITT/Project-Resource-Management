using System.Net;
using System.Net.Http.Json;

namespace Prm.Tests.Integration;

[Collection(nameof(IntegrationTestCollection))]
public class AdminProjectsIntegrationTests : PrmIntegrationTestBase
{
    public AdminProjectsIntegrationTests(PrmWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetAll_Returns_Seeded_Projects()
    {
        var projects = await Client.GetFromJsonAsync<List<ProjectListItem>>("/api/admin/projects");
        Assert.NotNull(projects);
        Assert.True(projects!.Count >= 4);
        Assert.Contains(projects, p => p.Name == "Alpha Portal");
    }

    [Fact]
    public async Task CreateProject_Succeeds()
    {
        var users = await Client.GetFromJsonAsync<List<UserListItem>>("/api/admin/users");
        var rohanUserId = users!.First(u => u.Username == "rohan.verma").Id;

        var response = await Client.PostAsJsonAsync("/api/admin/projects", new
        {
            name = "Integration Test Project",
            description = "Created by integration test",
            startDate = "2026-06-01",
            endDate = "2026-12-31",
            status = "Planned",
            managerUserId = rohanUserId,
            totalStoryPoints = 50
        });

        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateProject_With_Empty_Name_Returns_400()
    {
        var users = await Client.GetFromJsonAsync<List<UserListItem>>("/api/admin/users");
        var managerId = users!.First(u => u.Username == "ankit.shah").Id;

        var response = await Client.PostAsJsonAsync("/api/admin/projects", new
        {
            name = "   ",
            description = "Test",
            startDate = "2026-01-01",
            endDate = "2026-12-31",
            status = "Planned",
            managerUserId = managerId,
            totalStoryPoints = 10
        });

        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProject_With_Employee_As_Manager_Returns_400()
    {
        var users = await Client.GetFromJsonAsync<List<UserListItem>>("/api/admin/users");
        var raviUserId = users!.First(u => u.Username == "ravi.kumar").Id;

        var response = await Client.PostAsJsonAsync("/api/admin/projects", new
        {
            name = "Bad Manager Project",
            description = "Test",
            startDate = "2026-01-01",
            endDate = "2026-12-31",
            status = "Planned",
            managerUserId = raviUserId,
            totalStoryPoints = 10
        });

        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetMilestones_Returns_Project_Milestones()
    {
        var projects = await Client.GetFromJsonAsync<List<ProjectListItem>>("/api/admin/projects");
        var alpha = projects!.First(p => p.Name == "Alpha Portal");

        var milestones = await Client.GetFromJsonAsync<List<MilestoneDto>>($"/api/admin/projects/{alpha.Id}/milestones");
        Assert.NotNull(milestones);
        Assert.True(milestones!.Count >= 4);
    }

    [Fact]
    public async Task AddMilestone_Succeeds()
    {
        var projects = await Client.GetFromJsonAsync<List<ProjectListItem>>("/api/admin/projects");
        var delta = projects!.First(p => p.Name == "Delta Migrate");

        var response = await Client.PostAsJsonAsync($"/api/admin/projects/{delta.Id}/milestones", new
        {
            title = "Kickoff",
            dueDate = "2026-05-01",
            storyPoints = 10
        });

        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.OK);
    }

    [Fact]
    public async Task AddMilestone_With_DueDate_Outside_Project_Returns_400()
    {
        var projects = await Client.GetFromJsonAsync<List<ProjectListItem>>("/api/admin/projects");
        var projectId = projects!.First().Id;

        var response = await Client.PostAsJsonAsync($"/api/admin/projects/{projectId}/milestones", new
        {
            title = "Late Milestone",
            dueDate = "2030-01-01",
            storyPoints = 5
        });

        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddMilestone_Exceeding_StoryPoint_Budget_Returns_400()
    {
        var projects = await Client.GetFromJsonAsync<List<ProjectListItem>>("/api/admin/projects");
        var gamma = projects!.First(p => p.Name == "Gamma Rewrite");

        var response = await Client.PostAsJsonAsync($"/api/admin/projects/{gamma.Id}/milestones", new
        {
            title = "Overflow Milestone",
            dueDate = "2026-06-01",
            storyPoints = 500
        });

        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateMilestoneStatus_Succeeds()
    {
        var projects = await Client.GetFromJsonAsync<List<ProjectListItem>>("/api/admin/projects");
        var alpha = projects!.First(p => p.Name == "Alpha Portal");
        var milestones = await Client.GetFromJsonAsync<List<MilestoneDto>>($"/api/admin/projects/{alpha.Id}/milestones");
        var milestone = milestones!.First(m => m.Title == "Testing");

        var response = await Client.PutAsJsonAsync(
            $"/api/admin/projects/{alpha.Id}/milestones/{milestone.Id}/status",
            new { status = "InProgress" });

        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.OK);
    }

    private sealed record UserListItem(int Id, string Username, string FullName, string Role, bool IsActive);

    private sealed record ProjectListItem(int Id, string Name, string ManagerName, string EndDate, string Status, int StoryPointsDone, int TotalStoryPoints);

    private sealed record MilestoneDto(int Id, string Title, string DueDate, int StoryPoints, string Status);
}
