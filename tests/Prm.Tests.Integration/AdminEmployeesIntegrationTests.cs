using System.Net;
using System.Net.Http.Json;

namespace Prm.Tests.Integration;

[Collection(nameof(IntegrationTestCollection))]
public class AdminEmployeesIntegrationTests : PrmIntegrationTestBase
{
    public AdminEmployeesIntegrationTests(PrmWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetAll_Returns_Only_Active_Employees()
    {
        var response = await Client.GetFromJsonAsync<EmployeeListResponse>("/api/admin/employees");
        Assert.NotNull(response);
        Assert.All(response!.Employees, e => Assert.True(e.IsActive));
        Assert.DoesNotContain(response.Employees, e => e.Name == "Priya Sharma");
    }

    [Fact]
    public async Task GetAll_Derives_Sara_As_Allocated_With_75_Utilisation()
    {
        var response = await Client.GetFromJsonAsync<EmployeeListResponse>("/api/admin/employees");
        var sara = response!.Employees.First(e => e.Name == "Sara Khan");

        Assert.Equal("Allocated", sara.Status);
        Assert.Equal(75, sara.UtilisationPercent);
    }

    [Fact]
    public async Task UpdateEmployee_Department_Succeeds()
    {
        var employees = await Client.GetFromJsonAsync<EmployeeListResponse>("/api/admin/employees");
        var anil = employees!.Employees.First(e => e.Name == "Anil Mehta");

        var response = await Client.PutAsJsonAsync($"/api/admin/employees/{anil.Id}", new { department = "Engineering" });
        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateEmployee_With_Empty_Department_Returns_400()
    {
        var employees = await Client.GetFromJsonAsync<EmployeeListResponse>("/api/admin/employees");
        var anil = employees!.Employees.First(e => e.Name == "Anil Mehta");

        var response = await Client.PutAsJsonAsync($"/api/admin/employees/{anil.Id}", new { department = "   " });
        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddSkill_To_Inactive_Employee_Returns_400()
    {
        var employees = await Client.GetFromJsonAsync<EmployeeListResponse>("/api/admin/employees");
        var anil = employees!.Employees.First(e => e.Name == "Anil Mehta");

        var deactivate = await Client.PostAsync($"/api/admin/employees/{anil.Id}/deactivate", null);
        await PrmApiAssertions.AssertStatusAsync(deactivate, HttpStatusCode.OK);

        var response = await Client.PostAsJsonAsync($"/api/admin/employees/{anil.Id}/skills", new
        {
            skillName = "Angular",
            category = "Frontend",
            proficiency = "Beginner"
        });

        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddSkill_Succeeds()
    {
        var employees = await Client.GetFromJsonAsync<EmployeeListResponse>("/api/admin/employees");
        var anil = employees!.Employees.First(e => e.Name == "Anil Mehta");

        var response = await Client.PostAsJsonAsync($"/api/admin/employees/{anil.Id}/skills", new
        {
            skillName = "Terraform",
            category = "DevOps",
            proficiency = "Intermediate"
        });

        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.OK);
    }

    [Fact]
    public async Task AddDuplicateSkill_Returns_409()
    {
        var employees = await Client.GetFromJsonAsync<EmployeeListResponse>("/api/admin/employees");
        var ravi = employees!.Employees.First(e => e.Name == "Ravi Kumar");

        var response = await Client.PostAsJsonAsync($"/api/admin/employees/{ravi.Id}/skills", new
        {
            skillName = "Java",
            category = "Backend",
            proficiency = "Advanced"
        });

        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetSkills_Returns_Employee_Skills()
    {
        var employees = await Client.GetFromJsonAsync<EmployeeListResponse>("/api/admin/employees");
        var ravi = employees!.Employees.First(e => e.Name == "Ravi Kumar");

        var skills = await Client.GetFromJsonAsync<List<SkillDto>>($"/api/admin/employees/{ravi.Id}/skills");
        Assert.NotNull(skills);
        Assert.NotEmpty(skills!);
    }

    [Fact]
    public async Task AssignManager_Succeeds()
    {
        var employees = await Client.GetFromJsonAsync<EmployeeListResponse>("/api/admin/employees");
        var dev = employees!.Employees.First(e => e.Name == "Dev Patel");
        var users = await Client.GetFromJsonAsync<List<UserListItem>>("/api/admin/users");
        var nehaUserId = users!.First(u => u.Username == "neha.joshi").Id;

        var response = await Client.PutAsJsonAsync($"/api/admin/employees/{dev.Id}/assign-manager", new { managerUserId = nehaUserId });
        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeactivateEmployee_With_Allocations_Ends_Them()
    {
        var employees = await Client.GetFromJsonAsync<EmployeeListResponse>("/api/admin/employees");
        var sara = employees!.Employees.First(e => e.Name == "Sara Khan");

        var response = await Client.PostAsync($"/api/admin/employees/{sara.Id}/deactivate", null);
        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<DeactivateResponse>();
        Assert.True(body!.EndedAllocations >= 1);
    }

    [Fact]
    public async Task DeactivateManager_With_Active_Team_Returns_400()
    {
        var users = await Client.GetFromJsonAsync<List<UserListItem>>("/api/admin/users");
        var ankit = users!.First(u => u.Username == "ankit.shah");

        var response = await Client.PostAsync($"/api/admin/users/{ankit.Id}/deactivate", null);
        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.BadRequest);
    }

    private sealed record EmployeeListItem(int Id, string Name, string Department, string Status, bool IsActive, int UtilisationPercent);

    private sealed record EmployeeListResponse(List<EmployeeListItem> Employees, int Total, int AllocatedCount, int BenchCount);

    private sealed record UserListItem(int Id, string Username, string FullName, string Role, bool IsActive);

    private sealed record SkillDto(int SkillId, string Name, string Category, string Proficiency);

    private sealed record DeactivateResponse(string Message, int EndedAllocations);
}
