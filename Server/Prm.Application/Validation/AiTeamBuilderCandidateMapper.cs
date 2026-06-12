using Prm.Application.Common;
using Prm.Domain.Entities;
using Prm.Domain.Enums;

namespace Prm.Application.Validation;

public static class AiTeamBuilderCandidateMapper
{
    public record SkillSnapshot(string Name, string Proficiency);

    public record AllocationSnapshot(string ProjectName, DateOnly ToDate);

    public record TeamBuilderCandidateSnapshot(
        int EmployeeId,
        int UserId,
        string FullName,
        string? Designation,
        int UtilisationPercent,
        int AvailabilityPercent,
        IReadOnlyList<SkillSnapshot> Skills,
        IReadOnlyList<AllocationSnapshot> ActiveAllocations);

    public static TeamBuilderCandidateSnapshot Map(ResourceProfile resourceProfile, DateOnly today)
    {
        var utilisation = ActiveDateHelper.SumActiveUtilisation(resourceProfile.Allocations, today);
        var availability = ValidationConstants.MaxUtilisationPercent - utilisation;

        var activeAllocations = resourceProfile.Allocations
            .Where(allocation => ActiveDateHelper.IsAllocationActive(allocation, today))
            .Select(allocation => new AllocationSnapshot(
                allocation.Project.Name,
                allocation.ToDate))
            .ToList();

        var skills = resourceProfile.User.Skills
            .Select(userSkill => new SkillSnapshot(
                userSkill.Skill.Name,
                userSkill.Proficiency.ToString().ToUpperInvariant()))
            .ToList();

        return new TeamBuilderCandidateSnapshot(
            resourceProfile.Id,
            resourceProfile.UserId,
            resourceProfile.User.FullName,
            resourceProfile.User.Designation?.ToString(),
            utilisation,
            availability,
            skills,
            activeAllocations);
    }

    public static IReadOnlyList<TeamBuilderCandidateSnapshot> MapAll(
        IEnumerable<ResourceProfile> profiles,
        DateOnly today) =>
        profiles.Select(profile => Map(profile, today)).ToList();

    public static bool IsFullyBenched(TeamBuilderCandidateSnapshot candidate) =>
        candidate.UtilisationPercent == 0;
}
