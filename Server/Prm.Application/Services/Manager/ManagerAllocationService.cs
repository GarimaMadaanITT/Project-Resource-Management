using Microsoft.Extensions.Logging;
using Prm.Application.Common;
using Prm.Application.DTOs.Manager;
using Prm.Application.Interfaces;
using Prm.Application.Validation;
using Prm.Domain.Entities;
using Prm.Domain.Enums;

namespace Prm.Application.Services.Manager;

public class ManagerAllocationService : IManagerAllocationService
{
    private readonly IManagerContextService _context;
    private readonly IResourceProfileRepository _resourceProfiles;
    private readonly IProjectRepository _projects;
    private readonly IAllocationRepository _allocations;
    private readonly IAuditLogService _auditLog;
    private readonly ILogger<ManagerAllocationService> _logger;

    public ManagerAllocationService(
        IManagerContextService context,
        IResourceProfileRepository resourceProfiles,
        IProjectRepository projects,
        IAllocationRepository allocations,
        IAuditLogService auditLog,
        ILogger<ManagerAllocationService> logger)
    {
        _context = context;
        _resourceProfiles = resourceProfiles;
        _projects = projects;
        _allocations = allocations;
        _auditLog = auditLog;
        _logger = logger;
    }

    public async Task<ManagerAllocationDto> CreateAsync(
        int managerUserId,
        CreateManagerAllocationRequest request,
        CancellationToken cancellationToken = default)
    {
        var managerContext = await _context.ResolveAsync(managerUserId, cancellationToken);

        var project = EntityGuard.EnsureFound(
            await _projects.GetByIdAsync(request.ProjectId, cancellationToken),
            ErrorMessages.ProjectNotFound);
        ManagerScopeGuard.EnsureProjectOwnedByManager(project, managerUserId, _logger);

        var resourceProfile = EntityGuard.EnsureFound(
            await _resourceProfiles.GetTeamMemberAsync(
                managerContext.ManagerUserId,
                request.EmployeeId,
                cancellationToken),
            ErrorMessages.EmployeeNotFound);
        ManagerScopeGuard.EnsureEmployeeOnTeam(resourceProfile, managerContext.ManagerUserId, _logger, actingManagerUserId: managerUserId);

        var existingAllocations = await _allocations.GetByResourceProfileIdAsync(resourceProfile.Id, cancellationToken);
        AllocationValidator.ValidateCreateRequest(
            project,
            resourceProfile,
            request.UtilisationPercent,
            request.FromDate,
            request.ToDate,
            existingAllocations,
            _logger);

        var allocation = new Allocation
        {
            ResourceProfileId = resourceProfile.Id,
            ProjectId = project.Id,
            UtilisationPercent = request.UtilisationPercent,
            FromDate = request.FromDate,
            ToDate = request.ToDate
        };

        await _allocations.AddAsync(allocation, cancellationToken);
        await UpdateResourceStatusAsync(resourceProfile.Id, cancellationToken);

        await _auditLog.AuditAsync(
            AuditConstants.EntityNames.Allocation,
            allocation.Id,
            AuditConstants.Actions.Created,
            null,
            AuditSnapshotBuilder.AllocationSnapshot(allocation),
            managerUserId,
            AuthConstants.RoleName(UserRole.Manager),
            AuditConstants.Sources.User,
            cancellationToken);

        _logger.LogInformation(
            "Allocation created. AllocationId={AllocationId}, ProjectId={ProjectId}, ResourceProfileId={ResourceProfileId}, Utilisation={Utilisation}%, ManagerUserId={ManagerUserId}",
            allocation.Id,
            project.Id,
            resourceProfile.Id,
            request.UtilisationPercent,
            managerUserId);

        return new ManagerAllocationDto(
            allocation.Id,
            project.Id,
            project.Name,
            resourceProfile.Id,
            resourceProfile.User.FullName,
            allocation.UtilisationPercent,
            allocation.FromDate,
            allocation.ToDate);
    }

    public async Task<EndAllocationResponse> EndAsync(
        int managerUserId,
        int allocationId,
        EndAllocationRequest request,
        CancellationToken cancellationToken = default)
    {
        var allocation = EntityGuard.EnsureFound(
            await _allocations.GetByIdAsync(allocationId, cancellationToken),
            ErrorMessages.AllocationNotFound);

        ManagerScopeGuard.EnsureAllocationOnOwnedProject(allocation, managerUserId, _logger);

        var endDate = request.EndDate ?? ActiveDateHelper.TodayUtc;
        AllocationValidator.ValidateEndDate(allocation, endDate);

        var oldSnapshot = AuditSnapshotBuilder.AllocationSnapshot(allocation);

        allocation.ToDate = endDate;
        await _allocations.UpdateAsync(allocation, cancellationToken);

        await _auditLog.AuditAsync(
            AuditConstants.EntityNames.Allocation,
            allocation.Id,
            AuditConstants.Actions.Ended,
            oldSnapshot,
            AuditSnapshotBuilder.AllocationSnapshot(allocation),
            managerUserId,
            AuthConstants.RoleName(UserRole.Manager),
            AuditConstants.Sources.User,
            cancellationToken);

        var resourceProfile = EntityGuard.EnsureFound(
            await _resourceProfiles.GetByIdAsync(allocation.ResourceProfileId, cancellationToken),
            ErrorMessages.EmployeeNotFound);

        await UpdateResourceStatusAsync(resourceProfile.Id, cancellationToken);

        var activeAllocations = await _resourceProfiles.GetActiveAllocationsAsync(resourceProfile.Id, cancellationToken);
        var newUtilisation = ActiveDateHelper.SumActiveUtilisation(activeAllocations, ActiveDateHelper.TodayUtc);
        var newStatus = EmployeeStatusResolver.ResolveFromUtilisation(newUtilisation);

        _logger.LogInformation(
            "Allocation ended. AllocationId={AllocationId}, EndDate={EndDate}, ManagerUserId={ManagerUserId}",
            allocation.Id,
            endDate,
            managerUserId);

        _logger.LogInformation(
            "Resource status updated. ResourceProfileId={ResourceProfileId}, NewStatus={NewStatus}",
            resourceProfile.Id,
            newStatus);

        return new EndAllocationResponse(
            "Allocation ended successfully.",
            newStatus.ToString());
    }

    private async Task UpdateResourceStatusAsync(int resourceProfileId, CancellationToken cancellationToken)
    {
        var resourceProfile = EntityGuard.EnsureFound(
            await _resourceProfiles.GetByIdAsync(resourceProfileId, cancellationToken),
            ErrorMessages.EmployeeNotFound);

        var today = ActiveDateHelper.TodayUtc;
        var activeAllocations = await _resourceProfiles.GetActiveAllocationsAsync(resourceProfileId, cancellationToken);
        var utilisation = ActiveDateHelper.SumActiveUtilisation(activeAllocations, today);
        resourceProfile.ResourceStatus = EmployeeStatusResolver.ResolveFromUtilisation(utilisation);
        await _resourceProfiles.UpdateAsync(resourceProfile, cancellationToken);
    }
}
