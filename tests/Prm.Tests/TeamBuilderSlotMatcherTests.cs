using Prm.Application.Common;
using Prm.Application.DTOs.Manager;
using Prm.Application.Validation;
using Prm.Domain.Enums;

namespace Prm.Tests;

public class TeamBuilderSlotMatcherTests
{
    [Fact]
    public void MatchSlots_Assigns_DevOps_To_Garima_And_Java_To_Neha()
    {
        var garima = new AiTeamBuilderCandidateMapper.TeamBuilderCandidateSnapshot(
            1,
            1,
            "Garima Madaan",
            nameof(Designation.DevOpsEngineer),
            nameof(Department.DevOps),
            0,
            100,
            [
                new AiTeamBuilderCandidateMapper.SkillSnapshot("Java", "INTERMEDIATE"),
                new AiTeamBuilderCandidateMapper.SkillSnapshot("Docker", "ADVANCED"),
                new AiTeamBuilderCandidateMapper.SkillSnapshot("Kubernetes", "INTERMEDIATE")
            ],
            []);

        var neha = new AiTeamBuilderCandidateMapper.TeamBuilderCandidateSnapshot(
            2,
            2,
            "Neha Sharma",
            nameof(Designation.SoftwareEngineer),
            nameof(Department.Backend),
            0,
            100,
            [new AiTeamBuilderCandidateMapper.SkillSnapshot("Java", "INTERMEDIATE")],
            []);

        var assignable = new List<AiTeamBuilderCandidateMapper.TeamBuilderCandidateSnapshot> { garima, neha };
        var slots = TeamBuilderRequirementSlotParser.ToUnresolvedRoles(
            TeamBuilderRequirementSlotParser.ParseSlots("1 java developer and 1 devops for 5 months"));

        var matched = TeamBuilderSlotMatcher.MatchSlots(slots, assignable, assignable);

        var javaRole = Assert.Single(matched, role => role.RoleTitle == "Java Developer");
        var devOpsRole = Assert.Single(matched, role => role.RoleTitle == "DevOps Engineer");

        Assert.Equal(TeamBuilderConstants.StatusFilled, javaRole.Status);
        Assert.Equal("Neha Sharma", javaRole.AssignedEmployeeName);

        Assert.Equal(TeamBuilderConstants.StatusFilled, devOpsRole.Status);
        Assert.Equal("Garima Madaan", devOpsRole.AssignedEmployeeName);
    }

    [Fact]
    public void MatchSlots_Does_Not_Suggest_Allocated_Employees_As_Partial()
    {
        var benched = new AiTeamBuilderCandidateMapper.TeamBuilderCandidateSnapshot(
            1,
            1,
            "Bench Dev",
            nameof(Designation.SoftwareEngineer),
            nameof(Department.Backend),
            0,
            100,
            [new AiTeamBuilderCandidateMapper.SkillSnapshot("Java", "ADVANCED")],
            []);

        var allocated = new AiTeamBuilderCandidateMapper.TeamBuilderCandidateSnapshot(
            2,
            2,
            "Anil Mehta",
            nameof(Designation.DevOpsEngineer),
            nameof(Department.DevOps),
            50,
            50,
            [
                new AiTeamBuilderCandidateMapper.SkillSnapshot("Docker", "ADVANCED"),
                new AiTeamBuilderCandidateMapper.SkillSnapshot("Kubernetes", "INTERMEDIATE")
            ],
            []);

        var roles = TeamBuilderRequirementSlotParser.ToUnresolvedRoles(
            TeamBuilderRequirementSlotParser.ParseSlots("1 devops for 5 months"));

        var matched = TeamBuilderSlotMatcher.MatchSlots(roles, [benched], [benched, allocated]);

        Assert.Equal(TeamBuilderConstants.StatusGap, matched[0].Status);
        Assert.Null(matched[0].AssignedEmployeeName);
        Assert.NotEqual(TeamBuilderConstants.GapReasonAllocatedElsewhere, matched[0].Gap?.ReasonType);
    }
}
