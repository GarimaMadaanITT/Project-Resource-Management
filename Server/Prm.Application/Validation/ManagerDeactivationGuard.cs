using Prm.Domain.Exceptions;

namespace Prm.Application.Validation;

public static class ManagerDeactivationGuard
{
    public static void EnsureCanDeactivateManager(bool hasActiveTeamMembers, bool hasActiveProjects)
    {
        if (hasActiveTeamMembers && hasActiveProjects)
        {
            throw new DomainException(
                "Cannot deactivate manager: active employees still report to this manager and active projects are still assigned. Reassign employees and projects first.");
        }

        if (hasActiveTeamMembers)
        {
            throw new DomainException(
                "Cannot deactivate manager: active employees still report to this manager. Reassign employees first.");
        }

        if (hasActiveProjects)
        {
            throw new DomainException(
                "Cannot deactivate manager: active projects are still assigned to this manager. Reassign projects first.");
        }
    }
}
