using Prm.Application.Common;
using Prm.Domain.Entities;
using Prm.Domain.Enums;
using Prm.Domain.Exceptions;

namespace Prm.Application.Validation;

public static class ProjectValidator
{
    public static string ValidateName(string? name) =>
        StringGuard.RequireNonEmpty(name, "Project name");

    public static string NormalizeDescription(string? description) =>
        description?.Trim() ?? string.Empty;

    public static void ValidateDates(DateOnly startDate, DateOnly endDate)
    {
        if (startDate >= endDate)
        {
            throw new DomainException("Start date must be before end date.");
        }
    }

    public static void ValidateStoryPoints(int totalStoryPoints)
    {
        if (totalStoryPoints < 0)
        {
            throw new DomainException("Total story points cannot be negative.");
        }
    }

    public static void ValidateManager(User managerUser)
    {
        if (UserRoleHelper.GetPrimaryRole(managerUser) != UserRole.Manager)
        {
            throw new DomainException("Assigned user must be a Manager.");
        }

        if (!managerUser.IsActive)
        {
            throw new DomainException("Manager account is inactive.");
        }
    }

    public static void ValidateStoryPointBudget(int milestoneStoryPointsSum, int projectTotalStoryPoints)
    {
        if (milestoneStoryPointsSum > projectTotalStoryPoints)
        {
            throw new DomainException(
                $"Sum of milestone story points ({milestoneStoryPointsSum}) cannot exceed project total story points ({projectTotalStoryPoints}).");
        }
    }
}
