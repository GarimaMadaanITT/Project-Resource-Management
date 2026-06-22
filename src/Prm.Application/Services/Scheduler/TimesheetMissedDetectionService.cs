using Microsoft.Extensions.Logging;
using Prm.Application.Common;
using Prm.Application.Interfaces;
using Prm.Application.Validation;

namespace Prm.Application.Services.Scheduler;

public class TimesheetMissedDetectionService : ITimesheetMissedDetectionService
{
    private readonly IEmployeeRepository _employees;
    private readonly ITimesheetRepository _timesheets;
    private readonly IAuditLogService _auditLog;
    private readonly ILogger<TimesheetMissedDetectionService> _logger;

    public TimesheetMissedDetectionService(
        IEmployeeRepository employees,
        ITimesheetRepository timesheets,
        IAuditLogService auditLog,
        ILogger<TimesheetMissedDetectionService> logger)
    {
        _employees = employees;
        _timesheets = timesheets;
        _auditLog = auditLog;
        _logger = logger;
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var targetWeek = ActiveDateHelper.GetPreviousCompletedWeekStart();
        var employees = await _employees.GetAllActiveWithAllocationsAsync(cancellationToken);
        var detectedCount = 0;

        foreach (var employee in employees)
        {
            var hadAllocation = employee.Allocations
                .Any(allocation => ActiveDateHelper.IsAllocationActiveDuringWeek(allocation, targetWeek));

            if (!hadAllocation)
            {
                continue;
            }

            var exists = await _timesheets.ExistsForEmployeeWeekAsync(
                employee.Id,
                targetWeek,
                cancellationToken);

            if (exists)
            {
                continue;
            }

            await _auditLog.AuditAsync(
                AuditConstants.EntityNames.Timesheet,
                employee.Id,
                AuditConstants.Actions.MissedTimesheetDetected,
                null,
                AuditSnapshotBuilder.MissedTimesheetSnapshot(targetWeek, employee.Id),
                null,
                null,
                AuditConstants.Sources.Scheduler,
                cancellationToken);

            detectedCount++;

            _logger.LogInformation(
                "Missed timesheet detected. EmployeeId={EmployeeId}, WeekStart={WeekStart}",
                employee.Id,
                targetWeek);
        }

        _logger.LogInformation(
            "Missed timesheet detection completed. Detected={Detected}",
            detectedCount);

        return detectedCount;
    }
}
