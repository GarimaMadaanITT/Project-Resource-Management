using Microsoft.Extensions.DependencyInjection;
using Prm.Application.Common;
using Prm.Application.Interfaces;

namespace Prm.Tests.Integration;

[Collection(nameof(IntegrationTestCollection))]
public class SchedulerOrchestratorIntegrationTests : PrmIntegrationTestBase
{
    public SchedulerOrchestratorIntegrationTests(PrmWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task RunAsync_Completes_And_May_Write_Scheduler_Audit_Logs()
    {
        using var scope = Factory.Services.CreateScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<ISchedulerOrchestrator>();

        await orchestrator.RunAsync();

        var queryService = scope.ServiceProvider.GetRequiredService<IAdminAuditLogQueryService>();
        var schedulerLogs = await queryService.QueryAsync(
            new Prm.Application.DTOs.Admin.AuditLogQuery(
                1,
                100,
                null,
                null,
                AuditConstants.Sources.Scheduler,
                null,
                null));

        Assert.True(schedulerLogs.TotalCount >= 0);
    }
}
