using Prm.Application.Common;
using Prm.Application.DTOs.Manager;
using Prm.Application.Interfaces;
using Prm.Application.Validation;
using Prm.Domain.Exceptions;

namespace Prm.Application.Services.Manager;

public class ManagerTimesheetComplianceService : IManagerTimesheetComplianceService
{
    private readonly IManagerContextService _context;
    private readonly IResourceProfileRepository _resourceProfiles;
    private readonly IAuditLogService _auditLog;

    public ManagerTimesheetComplianceService(
        IManagerContextService context,
        IResourceProfileRepository resourceProfiles,
        IAuditLogService auditLog)
    {
        _context = context;
        _resourceProfiles = resourceProfiles;
        _auditLog = auditLog;
    }

    public async Task<RestoreTimesheetAccessResponse> RestoreTimesheetAccessAsync(
        int managerUserId,
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        var managerContext = await _context.ResolveAsync(managerUserId, cancellationToken);
        var resourceProfile = EntityGuard.EnsureFound(
            await _resourceProfiles.GetTeamMemberAsync(managerContext.ManagerUserId, employeeId, cancellationToken),
            ErrorMessages.EmployeeNotFound);

        ManagerScopeGuard.EnsureEmployeeOnTeam(resourceProfile, managerContext.ManagerUserId, actingManagerUserId: managerUserId);

        if (!resourceProfile.TimesheetSubmissionFrozen)
        {
            throw new DomainException("Timesheet submission access is not frozen for this employee.");
        }

        var oldSnapshot = AuditSnapshotBuilder.ResourceProfileSnapshot(resourceProfile);
        resourceProfile.TimesheetSubmissionFrozen = false;
        resourceProfile.TimesheetFrozenAt = null;
        await _resourceProfiles.UpdateAsync(resourceProfile, cancellationToken);

        await _auditLog.AuditAsync(
            AuditConstants.EntityNames.ResourceProfile,
            resourceProfile.Id,
            AuditConstants.Actions.TimesheetAccessRestored,
            oldSnapshot,
            AuditSnapshotBuilder.ResourceProfileSnapshot(resourceProfile),
            managerUserId,
            AuthConstants.RoleName(Domain.Enums.UserRole.Manager),
            AuditConstants.Sources.User,
            cancellationToken);

        return new RestoreTimesheetAccessResponse(
            resourceProfile.Id,
            resourceProfile.User.FullName,
            false,
            "Timesheet submission access restored.");
    }
}
