using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prm.Application.Common;
using Prm.Application.DTOs.Admin;
using Prm.Application.Interfaces;
using Prm.Infrastructure.Persistence.Seeding;

namespace Prm.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/scheduler")]
[Authorize(Policy = AuthConstants.PolicyNames.AdminOnly)]
public class AdminSchedulerController : ControllerBase
{
    private readonly ISchedulerOrchestrator _scheduler;
    private readonly ITimesheetMissedDetectionService _timesheetCompliance;
    private readonly DataSeeder _dataSeeder;

    public AdminSchedulerController(
        ISchedulerOrchestrator scheduler,
        ITimesheetMissedDetectionService timesheetCompliance,
        DataSeeder dataSeeder)
    {
        _scheduler = scheduler;
        _timesheetCompliance = timesheetCompliance;
        _dataSeeder = dataSeeder;
    }

    [HttpPost("run-now")]
    public async Task<ActionResult<SchedulerRunResponse>> RunNow(CancellationToken cancellationToken)
    {
        await _scheduler.RunAsync(cancellationToken);
        return Ok(new SchedulerRunResponse(
            "Scheduler completed. Check API logs and Mailtrap for notification emails."));
    }

    [HttpPost("seed-notification-test-data")]
    public async Task<ActionResult<NotificationTestDataSeedResponse>> SeedNotificationTestData(
        CancellationToken cancellationToken)
    {
        var result = await _dataSeeder.SeedNotificationTestDataAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("force-timesheet-compliance/{username}")]
    public async Task<ActionResult<TimesheetComplianceForceResponse>> ForceTimesheetCompliance(
        string username,
        CancellationToken cancellationToken)
    {
        var result = await _timesheetCompliance.ForceAdvanceComplianceAsync(username, cancellationToken);
        return Ok(result);
    }
}
