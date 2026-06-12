using Prm.Application.Common;
using Prm.Domain.Entities;

namespace Prm.Application.Validation;

public static class AiCapacityFilter
{
    public record CandidateSnapshot(
        ResourceProfile ResourceProfile,
        int UtilisationPercent,
        int AvailabilityPercent,
        int FreeHoursPerWeek,
        IReadOnlyList<string> ProfileSkills,
        IReadOnlyList<string> RecentActivityTags);

    public static IReadOnlyList<CandidateSnapshot> FilterTeam(
        IEnumerable<ResourceProfile> team,
        int maxWeeklyHours,
        int? requiredHoursPerWeek,
        DateOnly today)
    {
        var requiredFreeHours = requiredHoursPerWeek ?? (maxWeeklyHours / 2);
        var snapshots = new List<CandidateSnapshot>();

        foreach (var resourceProfile in team)
        {
            var utilisation = ActiveDateHelper.SumActiveUtilisation(resourceProfile.Allocations, today);
            var availability = ValidationConstants.MaxUtilisationPercent - utilisation;
            var freeHours = (int)Math.Floor(availability / 100m * maxWeeklyHours);

            if (freeHours < requiredFreeHours)
            {
                continue;
            }

            snapshots.Add(new CandidateSnapshot(
                resourceProfile,
                utilisation,
                availability,
                freeHours,
                resourceProfile.User.Skills.Select(skill => skill.Skill.Name).ToList(),
                []));
        }

        return snapshots;
    }
}
