using Microsoft.Extensions.Logging;
using Prm.Application.Common;
using Prm.Application.Interfaces;
using Prm.Application.Validation;
using Prm.Domain.Enums;

namespace Prm.Application.Services.Scheduler;

public class UtilisationRecomputeService : IUtilisationRecomputeService
{
    private readonly IEmployeeRepository _employees;
    private readonly IAuditLogService _auditLog;
    private readonly ILogger<UtilisationRecomputeService> _logger;

    public UtilisationRecomputeService(
        IEmployeeRepository employees,
        IAuditLogService auditLog,
        ILogger<UtilisationRecomputeService> logger)
    {
        _employees = employees;
        _auditLog = auditLog;
        _logger = logger;
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var today = ActiveDateHelper.TodayUtc;
        var employees = await _employees.GetAllActiveWithAllocationsAsync(cancellationToken);
        var updatedCount = 0;

        foreach (var employee in employees)
        {
            var oldSnapshot = AuditSnapshotBuilder.EmployeeSnapshot(employee);
            var utilisation = ActiveDateHelper.SumActiveUtilisation(employee.Allocations, today);
            var newStatus = EmployeeStatusResolver.ResolveFromUtilisation(utilisation);

            if (employee.Status == newStatus)
            {
                continue;
            }

            employee.Status = newStatus;
            await _employees.UpdateAsync(employee, cancellationToken);

            await _auditLog.AuditAsync(
                AuditConstants.EntityNames.Employee,
                employee.Id,
                AuditConstants.Actions.StatusChanged,
                oldSnapshot,
                AuditSnapshotBuilder.EmployeeSnapshot(employee),
                null,
                null,
                AuditConstants.Sources.Scheduler,
                cancellationToken);

            updatedCount++;
        }

        _logger.LogInformation(
            "Utilisation recompute completed. Processed={Processed}, Updated={Updated}",
            employees.Count,
            updatedCount);

        return updatedCount;
    }
}
