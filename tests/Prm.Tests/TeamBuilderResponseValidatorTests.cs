using Prm.Application.Common;
using Prm.Application.DTOs.Manager;
using Prm.Application.Validation;
using Prm.Domain.Exceptions;

namespace Prm.Tests;

public class TeamBuilderResponseValidatorTests
{
    private static readonly IReadOnlyList<AiTeamBuilderCandidateMapper.TeamBuilderCandidateSnapshot> AssignablePool =
    [
        new(
            1,
            101,
            "Anil Mehta",
            "DevOpsEngineer",
            "DevOps",
            0,
            100,
            [new AiTeamBuilderCandidateMapper.SkillSnapshot("Docker", "ADVANCED")],
            [])
    ];

    [Fact]
    public void Validate_Accepts_Valid_Filled_And_Gap_Roles()
    {
        var roles = new List<TeamBuilderRoleResultDto>
        {
            new(
                "DevOps Engineer",
                [                new TeamBuilderSkillRequirementDto("Docker", "INTERMEDIATE")],
                TeamBuilderConstants.StatusFilled,
                "Anil Mehta",
                90,
                "Fully benched match.",
                null,
                []),
            new(
                "QA Tester",
                [new TeamBuilderSkillRequirementDto("Selenium", TeamBuilderConstants.ProficiencyAny)],
                TeamBuilderConstants.StatusGap,
                null,
                null,
                null,
                new TeamBuilderGapDto(
                    TeamBuilderConstants.GapReasonNoSkill,
                    "No Selenium skills in org.",
                    null,
                    null),
                [])
        };

        TeamBuilderResponseValidator.Validate(roles, AssignablePool);
    }

    [Fact]
    public void Validate_Rejects_Duplicate_Assignee()
    {
        var roles = new List<TeamBuilderRoleResultDto>
        {
            new(
                "DevOps Engineer",
                [new TeamBuilderSkillRequirementDto("Docker", "INTERMEDIATE")],
                TeamBuilderConstants.StatusFilled,
                "Anil Mehta",
                90,
                "Match one.",
                null,
                []),
            new(
                "Platform Engineer",
                [new TeamBuilderSkillRequirementDto("Docker", "BEGINNER")],
                TeamBuilderConstants.StatusFilled,
                "Anil Mehta",
                80,
                "Match two.",
                null,
                [])
        };

        Assert.Throws<DomainException>(() => TeamBuilderResponseValidator.Validate(roles, AssignablePool));
    }

    [Fact]
    public void Validate_Rejects_Assignee_Not_In_Benched_Pool()
    {
        var roles = new List<TeamBuilderRoleResultDto>
        {
            new(
                "Java Developer",
                [new TeamBuilderSkillRequirementDto("Java", "ADVANCED")],
                TeamBuilderConstants.StatusFilled,
                "Dev Patel",
                85,
                "Should fail — not in assignable pool.",
                null,
                [])
        };

        Assert.Throws<DomainException>(() => TeamBuilderResponseValidator.Validate(roles, AssignablePool));
    }

    [Fact]
    public void Validate_Rejects_Invalid_Gap_Reason()
    {
        var roles = new List<TeamBuilderRoleResultDto>
        {
            new(
                "QA Tester",
                [new TeamBuilderSkillRequirementDto("Selenium", "BEGINNER")],
                TeamBuilderConstants.StatusGap,
                null,
                null,
                null,
                new TeamBuilderGapDto("UNKNOWN", "Bad reason.", null, null),
                [])
        };

        Assert.Throws<DomainException>(() => TeamBuilderResponseValidator.Validate(roles, AssignablePool));
    }
}
