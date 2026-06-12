using System.Net;
using System.Net.Http.Json;
using Prm.Application.Common;
using Prm.Application.DTOs.Admin;

namespace Prm.Tests.Integration;

[Collection(nameof(IntegrationTestCollection))]
public class AdminAuditLogsIntegrationTests : PrmIntegrationTestBase
{
    public AdminAuditLogsIntegrationTests(PrmWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetAll_Returns_Paginated_Audit_Logs()
    {
        var response = await Client.GetAsync("/api/admin/audit-logs?page=1&pageSize=10");
        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<AuditLogListResponse>();
        Assert.NotNull(result);
        Assert.Equal(1, result!.Page);
        Assert.Equal(10, result.PageSize);
        Assert.True(result.TotalCount >= 0);
    }

    [Fact]
    public async Task CreateUser_Writes_User_Created_Audit_Log()
    {
        var response = await Client.PostAsJsonAsync("/api/admin/users", new
        {
            fullName = "Audit Test Admin",
            email = "audit.test.admin@techserve.com",
            username = "audit.test.admin",
            temporaryPassword = "TestAdmin1",
            role = "Admin"
        });

        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.OK);

        var auditResponse = await Client.GetAsync(
            $"/api/admin/audit-logs?entityName={AuditConstants.EntityNames.User}&action={AuditConstants.Actions.Created}&pageSize=50");
        await PrmApiAssertions.AssertStatusAsync(auditResponse, HttpStatusCode.OK);

        var audit = await auditResponse.Content.ReadFromJsonAsync<AuditLogListResponse>();
        Assert.NotNull(audit);
        Assert.Contains(
            audit!.Items,
            item => item.EntityName == AuditConstants.EntityNames.User
                    && item.Action == AuditConstants.Actions.Created
                    && item.Source == AuditConstants.Sources.User
                    && item.NewValue != null
                    && item.NewValue.Contains("audit.test.admin"));
    }

    [Fact]
    public async Task Manager_Cannot_Access_Audit_Logs()
    {
        var client = Factory.CreateClient();
        var token = await Factory.LoginAsync(client, "ankit.shah", "Manager@1234");
        PrmWebApplicationFactory.Authorize(client, token);

        var response = await client.GetAsync("/api/admin/audit-logs");
        await PrmApiAssertions.AssertStatusAsync(response, HttpStatusCode.Forbidden);
    }
}
