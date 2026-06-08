using System.Net;
using System.Net.Http.Json;

namespace Prm.Tests.Integration;

[Collection(nameof(IntegrationTestCollection))]
public class AuthIntegrationTests : PrmIntegrationTestBase
{
    public AuthIntegrationTests(PrmWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Login_With_Empty_Username_Returns_400()
    {
        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { username = "", password = "Admin@1234" });
        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_With_Invalid_Credentials_Returns_401()
    {
        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { username = "admin", password = "WrongPassword1" });
        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_As_Manager_Returns_Token()
    {
        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { username = "ankit.shah", password = "Manager@1234" });
        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body?.Token);
        Assert.Equal("Manager", body!.User.Role);
    }

    [Fact]
    public async Task Login_As_Inactive_User_Returns_401()
    {
        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { username = "priya.sharma", password = "Employee@1234" });
        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Admin_With_ForcePasswordChange_Blocked_From_Admin_Api()
    {
        await Factory.ResetDatabaseAsync();
        var client = Factory.CreateClient();
        var token = await Factory.LoginAsync(client, "admin", "Admin@1234");
        PrmWebApplicationFactory.Authorize(client, token);

        var response = await client.GetAsync("/api/admin/users");
        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ChangePassword_Returns_New_Token()
    {
        await Factory.ResetDatabaseAsync();
        var client = Factory.CreateClient();
        var token = await Factory.LoginAsync(client, "admin", "Admin@1234");
        PrmWebApplicationFactory.Authorize(client, token);

        var response = await client.PostAsJsonAsync("/api/auth/change-password", new
        {
            newPassword = "Admin@9999",
            confirmPassword = "Admin@9999"
        });

        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ChangePasswordResponse>();
        Assert.False(body!.ForcePasswordChange);
        Assert.NotNull(body.Token);
    }

    [Fact]
    public async Task Me_Returns_Current_User()
    {
        var response = await Client.GetFromJsonAsync<MeResponse>("/api/auth/me");
        Assert.NotNull(response);
        Assert.Equal("admin", response!.Username);
        Assert.Equal("Admin", response.Role);
    }

    [Fact]
    public async Task AdminCheck_Returns_200_For_Admin()
    {
        var response = await Client.GetAsync("/api/auth/admin-check");
        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.OK);
    }

    [Fact]
    public async Task ManagerCheck_Returns_403_For_Admin()
    {
        var response = await Client.GetAsync("/api/auth/manager-check");
        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.Forbidden);
    }

    private sealed record LoginResponse(string Token, MeResponse User, bool ForcePasswordChange);

    private sealed record ChangePasswordResponse(string Message, bool ForcePasswordChange, string? Token);

    private sealed record MeResponse(int Id, string Username, string FullName, string Email, string Role);
}
