using Microsoft.Extensions.Logging;
using Prm.Application.Common;
using Prm.Application.Interfaces;
using Prm.Application.Validation;

namespace Prm.Application.Services.Scheduler;

public class UtilisationRecomputeService : IUtilisationRecomputeService
{
    private readonly IResourceProfileRepository _resourceProfiles;
    private readonly IAuditLogService _auditLog;
    private readonly ILogger<UtilisationRecomputeService> _logger;

    public UtilisationRecomputeService(
        IResourceProfileRepository resourceProfiles,
        IAuditLogService auditLog,
        ILogger<UtilisationRecomputeService> logger)
    {
        _resourceProfiles = resourceProfiles;
        _auditLog = auditLog;
        _logger = logger;
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var today = ActiveDateHelper.TodayUtc;
        var resourceProfiles = await _resourceProfiles.GetAllActiveWithAllocationsAsync(cancellationToken);
        var updatedCount = 0;

        foreach (var resourceProfile in resourceProfiles)
        {
            var oldSnapshot = AuditSnapshotBuilder.ResourceProfileSnapshot(resourceProfile);
            var utilisation = ActiveDateHelper.SumActiveUtilisation(resourceProfile.Allocations, today);
            var newStatus = EmployeeStatusResolver.ResolveFromUtilisation(utilisation);

            if (resourceProfile.ResourceStatus == newStatus)
            {
                continue;
            }

            resourceProfile.ResourceStatus = newStatus;
            await _resourceProfiles.UpdateAsync(resourceProfile, cancellationToken);

            await _auditLog.AuditAsync(
                AuditConstants.EntityNames.ResourceProfile,
                resourceProfile.Id,
                AuditConstants.Actions.StatusChanged,
                oldSnapshot,
                AuditSnapshotBuilder.ResourceProfileSnapshot(resourceProfile),
                null,
                null,
                AuditConstants.Sources.Scheduler,
                cancellationToken);

            updatedCount++;
        }

        _logger.LogInformation(
            "Utilisation recompute completed. Processed={Processed}, Updated={Updated}",
            resourceProfiles.Count,
            updatedCount);

        return updatedCount;
    }
}
