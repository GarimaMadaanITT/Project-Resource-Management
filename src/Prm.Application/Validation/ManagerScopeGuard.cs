using Microsoft.Extensions.Logging;
using Prm.Domain.Entities;
using Prm.Domain.Exceptions;
using Prm.Application.Common;

namespace Prm.Application.Validation;

public static class ManagerScopeGuard
{
    public static void EnsureEmployeeOnTeam(
        Employee teamMember,
        int managerEmployeeId,
        ILogger? logger = null,
        int? managerUserId = null)
    {
        if (teamMember.ManagerId != managerEmployeeId)
        {
            logger?.LogWarning(
                "Manager scope denied. ManagerUserId={ManagerUserId}, Resource=Employee, ResourceId={ResourceId}",
                managerUserId,
                teamMember.Id);

            throw new ForbiddenException(ErrorMessages.EmployeeNotOnManagerTeam);
        }
    }

    public static void EnsureProjectOwnedByManager(
        Project project,
        int managerUserId,
        ILogger? logger = null)
    {
        if (project.ManagerUserId != managerUserId)
        {
            logger?.LogWarning(
                "Manager scope denied. ManagerUserId={ManagerUserId}, Resource=Project, ResourceId={ResourceId}",
                managerUserId,
                project.Id);

            throw new ForbiddenException(ErrorMessages.ProjectNotOwnedByManager);
        }
    }

    public static void EnsureAllocationOnOwnedProject(
        Allocation allocation,
        int managerUserId,
        ILogger? logger = null)
    {
        if (allocation.Project.ManagerUserId != managerUserId)
        {
            logger?.LogWarning(
                "Manager scope denied. ManagerUserId={ManagerUserId}, Resource=Allocation, ResourceId={ResourceId}",
                managerUserId,
                allocation.Id);

            throw new ForbiddenException(ErrorMessages.ProjectNotOwnedByManager);
        }
    }
}
