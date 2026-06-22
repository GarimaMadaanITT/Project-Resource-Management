using Prm.Application.Common;
using Prm.Domain.Entities;
using Prm.Domain.Enums;

namespace Prm.Application.Validation;

public static class ProjectRiskFlagCalculator
{
    public record RiskFlag(string Code, string Message, bool IsRisk);

    public static IReadOnlyList<RiskFlag> Calculate(
        Project project,
        IReadOnlyList<Allocation> projectActiveAllocations,
        IReadOnlyDictionary<int, int> resourceProfileTotalUtilisation,
        IReadOnlyList<TimesheetEntry> recentEntries,
        int maxWeeklyHours,
        DateOnly today)
    {
        var flags = new List<RiskFlag>();
        flags.AddRange(GetMilestoneFlags(project.Milestones, today));
        flags.AddRange(GetLowHoursFlags(projectActiveAllocations, recentEntries, maxWeeklyHours));
        flags.Add(GetAllocationFlag(projectActiveAllocations, resourceProfileTotalUtilisation));

        return flags;
    }

    private static IEnumerable<RiskFlag> GetMilestoneFlags(IEnumerable<Milestone> milestones, DateOnly today)
    {
        foreach (var milestone in milestones.Where(m => m.Status != MilestoneStatus.Done && m.DueDate < today))
        {
            var daysOverdue = today.DayNumber - milestone.DueDate.DayNumber;
            yield return new RiskFlag(
                "MILESTONE_OVERDUE",
                $"{milestone.Title} milestone is {daysOverdue} day(s) overdue.",
                true);
        }
    }

    private static IEnumerable<RiskFlag> GetLowHoursFlags(
        IReadOnlyList<Allocation> activeAllocations,
        IReadOnlyList<TimesheetEntry> recentEntries,
        int maxWeeklyHours)
    {
        var lastWeekStart = ActiveDateHelper.GetCurrentWeekStartUtc().AddDays(-7);

        foreach (var allocation in activeAllocations)
        {
            var expectedHours = (allocation.UtilisationPercent / 100m) * maxWeeklyHours;
            if (expectedHours <= 0)
            {
                continue;
            }

            var loggedHours = recentEntries
                .Where(entry =>
                    entry.ProjectId == allocation.ProjectId
                    && entry.Timesheet.WeekStart == lastWeekStart
                    && entry.Timesheet.ResourceProfileId == allocation.ResourceProfileId)
                .Sum(entry => entry.Hours);

            if (loggedHours < expectedHours * 0.5m)
            {
                yield return new RiskFlag(
                    "LOW_HOURS",
                    $"{allocation.ResourceProfile.User.FullName} logged only {loggedHours} hrs last week (expected {expectedHours} hrs).",
                    true);
            }
        }
    }

    private static RiskFlag GetAllocationFlag(
        IReadOnlyList<Allocation> projectActiveAllocations,
        IReadOnlyDictionary<int, int> resourceProfileTotalUtilisation)
    {
        var overAllocated = projectActiveAllocations
            .Select(allocation => allocation.ResourceProfileId)
            .Distinct()
            .Any(resourceProfileId =>
                resourceProfileTotalUtilisation.TryGetValue(resourceProfileId, out var total)
                && total > ValidationConstants.MaxUtilisationPercent);

        if (overAllocated)
        {
            return new RiskFlag(
                "OVER_ALLOCATED",
                "One or more resources exceed 100% total utilisation.",
                true);
        }

        return new RiskFlag(
            "ALLOCATIONS_OK",
            "Resources are correctly allocated.",
            false);
    }
}
