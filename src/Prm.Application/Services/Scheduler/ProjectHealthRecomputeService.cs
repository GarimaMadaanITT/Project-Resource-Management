using Microsoft.Extensions.Logging;
using Prm.Application.Common;
using Prm.Application.Interfaces;
using Prm.Application.Validation;

namespace Prm.Application.Services.Scheduler;

public class ProjectHealthRecomputeService : IProjectHealthRecomputeService
{
    private readonly IProjectRepository _projects;
    private readonly IEmployeeRepository _employees;
    private readonly ITimesheetRepository _timesheets;
    private readonly ISystemSettingsRepository _settings;
    private readonly IAuditLogService _auditLog;
    private readonly ILogger<ProjectHealthRecomputeService> _logger;

    public ProjectHealthRecomputeService(
        IProjectRepository projects,
        IEmployeeRepository employees,
        ITimesheetRepository timesheets,
        ISystemSettingsRepository settings,
        IAuditLogService auditLog,
        ILogger<ProjectHealthRecomputeService> logger)
    {
        _projects = projects;
        _employees = employees;
        _timesheets = timesheets;
        _settings = settings;
        _auditLog = auditLog;
        _logger = logger;
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var today = ActiveDateHelper.TodayUtc;
        var lastWeekStart = ActiveDateHelper.GetCurrentWeekStartUtc().AddDays(-7);
        var settings = await _settings.GetAsync(cancellationToken);
        var projects = await _projects.GetAllActiveWithDetailsAsync(cancellationToken);
        var updatedCount = 0;

        foreach (var project in projects)
        {
            var projectActiveAllocations = project.Allocations
                .Where(allocation => ActiveDateHelper.IsAllocationActive(allocation, today))
                .ToList();

            var employeeIds = projectActiveAllocations
                .Select(allocation => allocation.EmployeeId)
                .Distinct()
                .ToList();

            var employeeTotalUtilisation = new Dictionary<int, int>();
            foreach (var employeeId in employeeIds)
            {
                var employee = await _employees.GetByIdAsync(employeeId, cancellationToken);
                if (employee is null)
                {
                    continue;
                }

                employeeTotalUtilisation[employeeId] =
                    ActiveDateHelper.SumActiveUtilisation(employee.Allocations, today);
            }

            var recentEntries = await _timesheets.GetEntriesForEmployeesAndWeekAsync(
                employeeIds,
                lastWeekStart,
                cancellationToken);

            var flags = ProjectRiskFlagCalculator.Calculate(
                project,
                projectActiveAllocations,
                employeeTotalUtilisation,
                recentEntries,
                settings.MaxWeeklyHours,
                today);

            var newHealth = ProjectHealthStatusResolver.Resolve(flags);
            if (project.HealthStatus == newHealth)
            {
                continue;
            }

            var oldSnapshot = AuditSnapshotBuilder.ProjectSnapshot(project);
            project.HealthStatus = newHealth;
            await _projects.UpdateAsync(project, cancellationToken);

            await _auditLog.AuditAsync(
                AuditConstants.EntityNames.Project,
                project.Id,
                AuditConstants.Actions.HealthChanged,
                oldSnapshot,
                AuditSnapshotBuilder.ProjectSnapshot(project),
                null,
                null,
                AuditConstants.Sources.Scheduler,
                cancellationToken);

            updatedCount++;
        }

        _logger.LogInformation(
            "Project health recompute completed. Processed={Processed}, Updated={Updated}",
            projects.Count,
            updatedCount);

        return updatedCount;
    }
}
