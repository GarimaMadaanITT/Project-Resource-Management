using System.Net;
using System.Net.Http.Json;

namespace Prm.Tests.Integration;

[Collection(nameof(IntegrationTestCollection))]
public class AdminAllocationsIntegrationTests : PrmIntegrationTestBase
{
    public AdminAllocationsIntegrationTests(PrmWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetAll_Returns_Active_Allocations()
    {
        var allocations = await Client.GetFromJsonAsync<List<AllocationItem>>("/api/admin/allocations");
        Assert.NotNull(allocations);
        Assert.NotEmpty(allocations!);
    }

    [Fact]
    public async Task GetAll_FilterBy_EmployeeId()
    {
        var employees = await Client.GetFromJsonAsync<EmployeeListResponse>("/api/admin/employees");
        var sara = employees!.Employees.First(e => e.Name == "Sara Khan");

        var allocations = await Client.GetFromJsonAsync<List<AllocationItem>>($"/api/admin/allocations?employeeId={sara.Id}");
        Assert.NotNull(allocations);
        Assert.Single(allocations!);
        Assert.Equal(75, allocations![0].UtilisationPercent);
    }

    [Fact]
    public async Task GetAll_FilterBy_ProjectId()
    {
        var projects = await Client.GetFromJsonAsync<List<ProjectItem>>("/api/admin/projects");
        var alpha = projects!.First(p => p.Name == "Alpha Portal");

        var allocations = await Client.GetFromJsonAsync<List<AllocationItem>>($"/api/admin/allocations?projectId={alpha.Id}");
        Assert.NotNull(allocations);
        Assert.True(allocations!.Count >= 1);
    }

    private sealed record AllocationItem(string EmployeeName, string ProjectName, int UtilisationPercent, string FromDate, string ToDate);

    private sealed record EmployeeListItem(int Id, string Name, string Department, string Status, bool IsActive, int UtilisationPercent);

    private sealed record EmployeeListResponse(List<EmployeeListItem> Employees, int Total, int AllocatedCount, int BenchCount);

    private sealed record ProjectItem(int Id, string Name, string ManagerName, string EndDate, string Status, int StoryPointsDone, int TotalStoryPoints);
}
