using Microsoft.Extensions.Logging;
using Prm.Application.Common;
using Prm.Application.Interfaces;
using Prm.Application.Services.Notifications;
using Prm.Application.Validation;

namespace Prm.Application.Services.Scheduler;

public class ProjectHealthRecomputeService : IProjectHealthRecomputeService
{
    private readonly IProjectRepository _projects;
    private readonly IResourceProfileRepository _resourceProfiles;
    private readonly ITimesheetRepository _timesheets;
    private readonly ISystemSettingsRepository _settings;
    private readonly IAuditLogService _auditLog;
    private readonly IProjectAtRiskNotificationService _atRiskNotification;
    private readonly ILogger<ProjectHealthRecomputeService> _logger;

    public ProjectHealthRecomputeService(
        IProjectRepository projects,
        IResourceProfileRepository resourceProfiles,
        ITimesheetRepository timesheets,
        ISystemSettingsRepository settings,
        IAuditLogService auditLog,
        IProjectAtRiskNotificationService atRiskNotification,
        ILogger<ProjectHealthRecomputeService> logger)
    {
        _projects = projects;
        _resourceProfiles = resourceProfiles;
        _timesheets = timesheets;
        _settings = settings;
        _auditLog = auditLog;
        _atRiskNotification = atRiskNotification;
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

            var resourceProfileIds = projectActiveAllocations
                .Select(allocation => allocation.ResourceProfileId)
                .Distinct()
                .ToList();

            var resourceProfileTotalUtilisation = new Dictionary<int, int>();
            foreach (var resourceProfileId in resourceProfileIds)
            {
                var resourceProfile = await _resourceProfiles.GetByIdAsync(resourceProfileId, cancellationToken);
                if (resourceProfile is null)
                {
                    continue;
                }

                resourceProfileTotalUtilisation[resourceProfileId] =
                    ActiveDateHelper.SumActiveUtilisation(resourceProfile.Allocations, today);
            }

            var recentEntries = await _timesheets.GetEntriesForResourceProfilesAndWeekAsync(
                resourceProfileIds,
                lastWeekStart,
                cancellationToken);

            var flags = ProjectRiskFlagCalculator.Calculate(
                project,
                projectActiveAllocations,
                resourceProfileTotalUtilisation,
                recentEntries,
                settings.MaxWeeklyHours,
                today);

            var previousHealth = project.HealthStatus;
            var newHealth = ProjectHealthStatusResolver.Resolve(flags);
            if (previousHealth == newHealth)
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

            if (newHealth == Domain.Enums.HealthStatus.AtRisk)
            {
                try
                {
                    await _atRiskNotification.NotifyAsync(project, flags, previousHealth, cancellationToken);
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "At-risk notification failed. ProjectId={ProjectId}",
                        project.Id);
                }
            }

            updatedCount++;
        }

        _logger.LogInformation(
            "Project health recompute completed. Processed={Processed}, Updated={Updated}",
            projects.Count,
            updatedCount);

        return updatedCount;
    }
}
