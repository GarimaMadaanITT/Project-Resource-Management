using Prm.Application.Common;
using Prm.Application.DTOs.Manager;
using Prm.Application.Validation;
using Prm.Domain.Enums;

namespace Prm.Tests;

public class AiTeamBuilderSkillRankerTests
{
    [Fact]
    public void EnrichAndCorrectRoles_Fills_Java_Developer_From_Benched_Pool()
    {
        var assignable = new List<AiTeamBuilderCandidateMapper.TeamBuilderCandidateSnapshot>
        {
            new(9, 9, "Java Bench Dev", "SoftwareEngineer", "Backend", 0, 100,
                [new AiTeamBuilderCandidateMapper.SkillSnapshot("Java", "ADVANCED")], []),
            new(3, 3, "Anil Mehta", "DevOpsEngineer", "DevOps", 0, 100,
                [new AiTeamBuilderCandidateMapper.SkillSnapshot("Docker", "ADVANCED")], [])
        };

        var roles = AiTeamBuilderRequirementAnalyzer.ParseRequirementToRoles("I need a java developer");
        var enriched = AiTeamBuilderSkillRanker.EnrichAndCorrectRoles(roles, assignable, assignable);

        Assert.Single(enriched);
        Assert.Equal(TeamBuilderConstants.StatusFilled, enriched[0].Status);
        Assert.Equal("Java Bench Dev", enriched[0].AssignedEmployeeName);
        Assert.Contains(enriched[0].BenchMatches, match => match.UserId == 9);
        Assert.Equal(TeamBuilderConstants.ProficiencyAny, enriched[0].RequiredSkills[0].MinProficiency);
    }

    [Fact]
    public void InferProficiency_Returns_Any_When_Not_Specified()
    {
        Assert.Equal(TeamBuilderConstants.ProficiencyAny,
            AiTeamBuilderSkillRanker.InferProficiency("I need a java developer"));
        Assert.Equal("ADVANCED",
            AiTeamBuilderSkillRanker.InferProficiency("senior java developer with advanced java"));
    }

    [Fact]
    public void EnrichAndCorrectRoles_Fills_Sdet_From_Selenium_Skill_In_Qa_Department()
    {
        var garima = new AiTeamBuilderCandidateMapper.TeamBuilderCandidateSnapshot(
            9,
            10,
            "Garima Madaan",
            nameof(Designation.QAEngineer),
            nameof(Department.QA),
            0,
            100,
            [new AiTeamBuilderCandidateMapper.SkillSnapshot("Selenium", "ADVANCED")],
            []);

        var roles = new List<TeamBuilderRoleResultDto>
        {
            new(
                "SDET",
                [new TeamBuilderSkillRequirementDto("SDET", TeamBuilderConstants.ProficiencyAny)],
                TeamBuilderConstants.StatusGap,
                null,
                null,
                null,
                null,
                [])
        };

        var enriched = AiTeamBuilderSkillRanker.EnrichAndCorrectRoles(roles, [garima], [garima]);

        Assert.Equal(TeamBuilderConstants.StatusFilled, enriched[0].Status);
        Assert.Equal("Garima Madaan", enriched[0].AssignedEmployeeName);
        Assert.Contains(enriched[0].BenchMatches, match => match.EmployeeName == "Garima Madaan");
    }
}
