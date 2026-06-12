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
    private readonly IEmployeeRepository _employees;
    private readonly IProjectRepository _projects;
    private readonly IAllocationRepository _allocations;
    private readonly IAuditLogService _auditLog;
    private readonly ILogger<ManagerAllocationService> _logger;

    public ManagerAllocationService(
        IManagerContextService context,
        IEmployeeRepository employees,
        IProjectRepository projects,
        IAllocationRepository allocations,
        IAuditLogService auditLog,
        ILogger<ManagerAllocationService> logger)
    {
        _context = context;
        _employees = employees;
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

        var employee = EntityGuard.EnsureFound(
            await _employees.GetTeamMemberAsync(
                managerContext.ManagerEmployeeId,
                request.EmployeeId,
                cancellationToken),
            ErrorMessages.EmployeeNotFound);
        ManagerScopeGuard.EnsureEmployeeOnTeam(employee, managerContext.ManagerEmployeeId, _logger, managerUserId);

        var existingAllocations = await _allocations.GetByEmployeeIdAsync(employee.Id, cancellationToken);
        AllocationValidator.ValidateCreateRequest(
            project,
            employee,
            request.UtilisationPercent,
            request.FromDate,
            request.ToDate,
            existingAllocations,
            _logger);

        var allocation = new Allocation
        {
            EmployeeId = employee.Id,
            ProjectId = project.Id,
            UtilisationPercent = request.UtilisationPercent,
            FromDate = request.FromDate,
            ToDate = request.ToDate
        };

        await _allocations.AddAsync(allocation, cancellationToken);
        await UpdateEmployeeStatusAsync(employee.Id, cancellationToken);

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
            "Allocation created. AllocationId={AllocationId}, ProjectId={ProjectId}, EmployeeId={EmployeeId}, Utilisation={Utilisation}%, ManagerUserId={ManagerUserId}",
            allocation.Id,
            project.Id,
            employee.Id,
            request.UtilisationPercent,
            managerUserId);

        return new ManagerAllocationDto(
            allocation.Id,
            project.Id,
            project.Name,
            employee.Id,
            employee.User.FullName,
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

        var employee = EntityGuard.EnsureFound(
            await _employees.GetByIdAsync(allocation.EmployeeId, cancellationToken),
            ErrorMessages.EmployeeNotFound);

        await UpdateEmployeeStatusAsync(employee.Id, cancellationToken);

        var activeAllocations = await _employees.GetActiveAllocationsAsync(employee.Id, cancellationToken);
        var newUtilisation = ActiveDateHelper.SumActiveUtilisation(activeAllocations, ActiveDateHelper.TodayUtc);
        var newStatus = EmployeeStatusResolver.ResolveFromUtilisation(newUtilisation);

        _logger.LogInformation(
            "Allocation ended. AllocationId={AllocationId}, EndDate={EndDate}, ManagerUserId={ManagerUserId}",
            allocation.Id,
            endDate,
            managerUserId);

        _logger.LogInformation(
            "Employee status updated. EmployeeId={EmployeeId}, NewStatus={NewStatus}",
            employee.Id,
            newStatus);

        return new EndAllocationResponse(
            "Allocation ended successfully.",
            newStatus.ToString());
    }

    private async Task UpdateEmployeeStatusAsync(int employeeId, CancellationToken cancellationToken)
    {
        var employee = EntityGuard.EnsureFound(
            await _employees.GetByIdAsync(employeeId, cancellationToken),
            ErrorMessages.EmployeeNotFound);

        var today = ActiveDateHelper.TodayUtc;
        var activeAllocations = await _employees.GetActiveAllocationsAsync(employeeId, cancellationToken);
        var utilisation = ActiveDateHelper.SumActiveUtilisation(activeAllocations, today);
        employee.Status = EmployeeStatusResolver.ResolveFromUtilisation(utilisation);
        await _employees.UpdateAsync(employee, cancellationToken);
    }
}
