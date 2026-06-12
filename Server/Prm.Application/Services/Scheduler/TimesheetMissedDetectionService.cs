using Microsoft.Extensions.Logging;
using Prm.Application.Common;
using Prm.Application.Interfaces;
using Prm.Application.Validation;

namespace Prm.Application.Services.Scheduler;

public class TimesheetMissedDetectionService : ITimesheetMissedDetectionService
{
    private readonly IResourceProfileRepository _resourceProfiles;
    private readonly ITimesheetRepository _timesheets;
    private readonly IAuditLogService _auditLog;
    private readonly ILogger<TimesheetMissedDetectionService> _logger;

    public TimesheetMissedDetectionService(
        IResourceProfileRepository resourceProfiles,
        ITimesheetRepository timesheets,
        IAuditLogService auditLog,
        ILogger<TimesheetMissedDetectionService> logger)
    {
        _resourceProfiles = resourceProfiles;
        _timesheets = timesheets;
        _auditLog = auditLog;
        _logger = logger;
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var targetWeek = ActiveDateHelper.GetPreviousCompletedWeekStart();
        var resourceProfiles = await _resourceProfiles.GetAllActiveWithAllocationsAsync(cancellationToken);
        var detectedCount = 0;

        foreach (var resourceProfile in resourceProfiles)
        {
            var hadAllocation = resourceProfile.Allocations
                .Any(allocation => ActiveDateHelper.IsAllocationActiveDuringWeek(allocation, targetWeek));

            if (!hadAllocation)
            {
                continue;
            }

            var exists = await _timesheets.ExistsForResourceProfileWeekAsync(
                resourceProfile.Id,
                targetWeek,
                cancellationToken);

            if (exists)
            {
                continue;
            }

            await _auditLog.AuditAsync(
                AuditConstants.EntityNames.Timesheet,
                resourceProfile.Id,
                AuditConstants.Actions.MissedTimesheetDetected,
                null,
                AuditSnapshotBuilder.MissedTimesheetSnapshot(targetWeek, resourceProfile.Id),
                null,
                null,
                AuditConstants.Sources.Scheduler,
                cancellationToken);

            detectedCount++;

            _logger.LogInformation(
                "Missed timesheet detected. ResourceProfileId={ResourceProfileId}, WeekStart={WeekStart}",
                resourceProfile.Id,
                targetWeek);
        }

        _logger.LogInformation(
            "Missed timesheet detection completed. Detected={Detected}",
            detectedCount);

        return detectedCount;
    }
}
