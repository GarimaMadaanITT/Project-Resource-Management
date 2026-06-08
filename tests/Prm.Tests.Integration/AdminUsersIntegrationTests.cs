using System.Net;
using System.Net.Http.Json;

namespace Prm.Tests.Integration;

[Collection(nameof(IntegrationTestCollection))]
public class AdminUsersIntegrationTests : PrmIntegrationTestBase
{
    public AdminUsersIntegrationTests(PrmWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetAll_Returns_Seeded_Users()
    {
        var users = await Client.GetFromJsonAsync<List<UserListItem>>("/api/admin/users");
        Assert.NotNull(users);
        Assert.True(users!.Count >= 9);
        Assert.Contains(users, u => u.Username == "admin");
        Assert.Contains(users, u => u.Username == "priya.sharma" && !u.IsActive);
    }

    [Fact]
    public async Task CreateUser_Admin_Succeeds()
    {
        var response = await Client.PostAsJsonAsync("/api/admin/users", new
        {
            fullName = "Test Admin",
            email = "test.admin@techserve.com",
            username = "test.admin",
            temporaryPassword = "TestAdmin1",
            role = "Admin"
        });

        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateUser_With_Invalid_Email_Returns_400()
    {
        var response = await Client.PostAsJsonAsync("/api/admin/users", new
        {
            fullName = "Bad Email",
            email = "not-an-email",
            username = "bad.email.user",
            temporaryPassword = "TestAdmin1",
            role = "Admin"
        });

        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateUser_With_Missing_Department_For_Manager_Returns_400()
    {
        var response = await Client.PostAsJsonAsync("/api/admin/users", new
        {
            fullName = "Bad Manager",
            email = "bad.manager@test.com",
            username = "bad.manager",
            temporaryPassword = "Manager@99",
            role = "Manager",
            department = ""
        });

        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateUser_With_Duplicate_Username_Returns_409()
    {
        var response = await Client.PostAsJsonAsync("/api/admin/users", new
        {
            fullName = "Duplicate Admin",
            email = "duplicate.admin@test.com",
            username = "admin",
            temporaryPassword = "Admin@1234",
            role = "Admin"
        });

        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ResetPassword_With_Weak_Password_Returns_400()
    {
        var users = await Client.GetFromJsonAsync<List<UserListItem>>("/api/admin/users");
        var ravi = users!.First(u => u.Username == "ravi.kumar");

        var response = await Client.PostAsJsonAsync($"/api/admin/users/{ravi.Id}/reset-password", new
        {
            newTemporaryPassword = "12345678"
        });

        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_Succeeds()
    {
        var users = await Client.GetFromJsonAsync<List<UserListItem>>("/api/admin/users");
        var anil = users!.First(u => u.Username == "anil.mehta");

        var response = await Client.PostAsJsonAsync($"/api/admin/users/{anil.Id}/reset-password", new
        {
            newTemporaryPassword = "ResetPass1"
        });

        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeactivateSelf_Returns_403()
    {
        var me = await Client.GetFromJsonAsync<MeResponse>("/api/auth/me");
        var response = await Client.PostAsync($"/api/admin/users/{me!.Id}/deactivate", null);
        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeactivateManager_With_Team_And_Projects_Returns_400()
    {
        var users = await Client.GetFromJsonAsync<List<UserListItem>>("/api/admin/users");
        var ankit = users!.First(u => u.Username == "ankit.shah");

        var response = await Client.PostAsync($"/api/admin/users/{ankit.Id}/deactivate", null);
        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Reactivate_User_Succeeds()
    {
        var users = await Client.GetFromJsonAsync<List<UserListItem>>("/api/admin/users");
        var anil = users!.First(u => u.Username == "anil.mehta");

        var deactivate = await Client.PostAsync($"/api/admin/users/{anil.Id}/deactivate", null);
        await PrmApiAssertions.AssertStatusAsync(deactivate, HttpStatusCode.OK);

        var response = await Client.PostAsync($"/api/admin/users/{anil.Id}/reactivate", null);
        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.OK);
    }

    private sealed record UserListItem(int Id, string Username, string FullName, string Role, bool IsActive);

    private sealed record MeResponse(int Id, string Username, string FullName, string Email, string Role);
}
