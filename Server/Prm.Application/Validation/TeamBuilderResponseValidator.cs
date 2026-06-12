using Prm.Application.Common;
using Prm.Domain.Enums;
using Prm.Domain.Exceptions;

namespace Prm.Application.Validation;

public static class TeamBuilderResponseValidator
{
    // Post-validation guards against non-deterministic LLM output (duplicate assignees, partial bench).
    public static void Validate(
        IReadOnlyList<Prm.Application.DTOs.Manager.TeamBuilderRoleResultDto> roles,
        IReadOnlyList<AiTeamBuilderCandidateMapper.TeamBuilderCandidateSnapshot> assignableCandidates)
    {
        if (roles.Count == 0)
        {
            throw new DomainException("Team builder response must include at least one role.");
        }

        var assignableByName = assignableCandidates
            .ToDictionary(candidate => candidate.FullName, StringComparer.OrdinalIgnoreCase);

        var assignedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var role in roles)
        {
            ValidateRole(role, assignableByName, assignedNames);
        }
    }

    private static void ValidateRole(
        Prm.Application.DTOs.Manager.TeamBuilderRoleResultDto role,
        IReadOnlyDictionary<string, AiTeamBuilderCandidateMapper.TeamBuilderCandidateSnapshot> assignableByName,
        HashSet<string> assignedNames)
    {
        if (string.IsNullOrWhiteSpace(role.RoleTitle))
        {
            throw new DomainException("Each team builder role must have a role title.");
        }

        if (role.RequiredSkills.Count == 0)
        {
            throw new DomainException($"Role '{role.RoleTitle}' must include at least one required skill.");
        }

        foreach (var skill in role.RequiredSkills)
        {
            if (string.IsNullOrWhiteSpace(skill.SkillName))
            {
                throw new DomainException($"Role '{role.RoleTitle}' has an empty skill name.");
            }

            if (!IsValidProficiency(skill.MinProficiency))
            {
                throw new DomainException(
                    $"Role '{role.RoleTitle}' has invalid proficiency '{skill.MinProficiency}'.");
            }
        }

        if (role.Status == TeamBuilderConstants.StatusFilled)
        {
            ValidateFilledRole(role, assignableByName, assignedNames);
            return;
        }

        if (role.Status == TeamBuilderConstants.StatusGap)
        {
            ValidateGapRole(role);
            return;
        }

        throw new DomainException(
            $"Role '{role.RoleTitle}' has invalid status '{role.Status}'.");
    }

    private static void ValidateFilledRole(
        Prm.Application.DTOs.Manager.TeamBuilderRoleResultDto role,
        IReadOnlyDictionary<string, AiTeamBuilderCandidateMapper.TeamBuilderCandidateSnapshot> assignableByName,
        HashSet<string> assignedNames)
    {
        if (string.IsNullOrWhiteSpace(role.AssignedEmployeeName))
        {
            throw new DomainException($"FILLED role '{role.RoleTitle}' must include assignedEmployeeName.");
        }

        if (role.Gap is not null)
        {
            throw new DomainException($"FILLED role '{role.RoleTitle}' must not include a gap.");
        }

        if (!assignedNames.Add(role.AssignedEmployeeName))
        {
            throw new DomainException(
                $"Employee '{role.AssignedEmployeeName}' is assigned to more than one role.");
        }

        if (!assignableByName.TryGetValue(role.AssignedEmployeeName, out var candidate))
        {
            throw new DomainException(
                $"Assigned employee '{role.AssignedEmployeeName}' is not in the fully benched pool.");
        }

        if (!AiTeamBuilderCandidateMapper.IsFullyBenched(candidate))
        {
            throw new DomainException(
                $"Assigned employee '{role.AssignedEmployeeName}' is not fully benched.");
        }
    }

    private static void ValidateGapRole(Prm.Application.DTOs.Manager.TeamBuilderRoleResultDto role)
    {
        if (!string.IsNullOrWhiteSpace(role.AssignedEmployeeName))
        {
            throw new DomainException($"GAP role '{role.RoleTitle}' must not include assignedEmployeeName.");
        }

        if (role.Gap is null)
        {
            throw new DomainException($"GAP role '{role.RoleTitle}' must include gap details.");
        }

        if (role.Gap.ReasonType is not (
            TeamBuilderConstants.GapReasonNoSkill or TeamBuilderConstants.GapReasonAllocatedElsewhere))
        {
            throw new DomainException(
                $"Role '{role.RoleTitle}' has invalid gap reason '{role.Gap.ReasonType}'.");
        }

        if (string.IsNullOrWhiteSpace(role.Gap.Message))
        {
            throw new DomainException($"GAP role '{role.RoleTitle}' must include a gap message.");
        }
    }

    private static bool IsValidProficiency(string proficiency) =>
        proficiency == TeamBuilderConstants.ProficiencyAny
        || Enum.TryParse<ProficiencyLevel>(proficiency, true, out _);
}
