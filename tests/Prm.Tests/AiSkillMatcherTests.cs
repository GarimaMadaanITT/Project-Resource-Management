using Prm.Application.Validation;
using Prm.Domain.Enums;

namespace Prm.Tests;

public class AiSkillMatcherTests
{
    [Fact]
    public void SkillMatchesKeyword_Links_Sdet_To_Selenium()
    {
        Assert.True(AiSkillMatcher.SkillMatchesKeyword("Selenium", "SDET"));
    }

    [Fact]
    public void SkillMatchesKeyword_Links_Python_To_Numpy()
    {
        Assert.True(AiSkillMatcher.SkillMatchesKeyword("NumPy", "Python"));
    }

    [Fact]
    public void ScoreCandidateSkills_Adds_Department_Bonus_For_Qa_Sdet()
    {
        var score = AiSkillMatcher.ScoreCandidateSkills(
            ["Selenium"],
            AiSkillMatcher.ExpandKeywords(["SDET"]),
            nameof(Department.QA),
            nameof(Designation.QAEngineer));

        Assert.True(score >= 50);
    }

    [Fact]
    public void ExpandKeywords_Includes_Aliases()
    {
        var expanded = AiSkillMatcher.ExpandKeywords(["python"]);
        Assert.Contains(expanded, item => item.Equals("numpy", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(expanded, item => item.Equals("django", StringComparison.OrdinalIgnoreCase));
    }
}

public class AiSkillMatchRankerTests
{
    [Fact]
    public void RankMatches_Prefers_Selenium_Dev_For_Sdet_Requirement()
    {
        var seleniumDev = BuildCandidate(9, "Garima Madaan", "QA", "QAEngineer", ["Selenium", "Java"]);
        var javaDev = BuildCandidate(4, "Other Dev", "Backend", "SoftwareEngineer", ["Java"]);

        var ranked = AiSkillMatchRanker.RankMatches(
            "I need an SDET",
            [seleniumDev, javaDev],
            []);

        Assert.NotEmpty(ranked);
        Assert.Equal(9, ranked[0].EmployeeId);
        Assert.Contains(ranked[0].MatchedSkills!, skill => skill.Contains("Selenium", StringComparison.OrdinalIgnoreCase));
        Assert.True(ranked[0].MatchScore > ranked[1].MatchScore);
    }

    private static AiCapacityFilter.CandidateSnapshot BuildCandidate(
        int id,
        string name,
        string department,
        string designation,
        IReadOnlyList<string> skills)
    {
        var user = TestDataHelpers.CreateUser(UserRole.Employee, fullName: name);
        user.Id = id;
        user.Department = department;
        user.Designation = designation;
        user.Skills = skills.Select((skill, index) => new Domain.Entities.UserSkill
        {
            UserId = id,
            SkillId = index + 1,
            Skill = new Domain.Entities.Skill { Id = index + 1, Name = skill, Category = SkillCategory.Qa }
        }).ToList();

        var profile = new Domain.Entities.ResourceProfile
        {
            Id = id,
            UserId = id,
            User = user,
            Allocations = []
        };

        return new AiCapacityFilter.CandidateSnapshot(
            profile,
            0,
            100,
            40,
            skills.ToList(),
            []);
    }
}
