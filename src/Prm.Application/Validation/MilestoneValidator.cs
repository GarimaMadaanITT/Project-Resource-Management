using Prm.Domain.Entities;
using Prm.Domain.Exceptions;

namespace Prm.Application.Validation;

public static class MilestoneValidator
{
    public static string ValidateTitle(string? title) =>
        StringGuard.RequireNonEmpty(title, "Milestone title");

    public static void ValidateStoryPoints(int storyPoints)
    {
        if (storyPoints < 0)
        {
            throw new DomainException("Milestone story points cannot be negative.");
        }
    }

    public static void ValidateDueDateWithinProject(DateOnly dueDate, Project project)
    {
        if (dueDate < project.StartDate || dueDate > project.EndDate)
        {
            throw new DomainException(
                $"Milestone due date must be within the project duration ({project.StartDate:yyyy-MM-dd} to {project.EndDate:yyyy-MM-dd}).");
        }
    }

    public static void ValidateStoryPointBudget(int existingSum, int additionalStoryPoints, int projectTotalStoryPoints)
    {
        var projectedTotal = existingSum + additionalStoryPoints;
        if (projectedTotal > projectTotalStoryPoints)
        {
            throw new DomainException(
                $"Adding {additionalStoryPoints} story points would exceed the project total of {projectTotalStoryPoints} (current milestone sum: {existingSum}).");
        }
    }
}
