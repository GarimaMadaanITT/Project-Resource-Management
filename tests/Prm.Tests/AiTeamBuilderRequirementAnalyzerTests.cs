using Prm.Application.Validation;

namespace Prm.Tests;

public class AiTeamBuilderRequirementAnalyzerTests
{
    [Fact]
    public void SplitRoleSegments_Parses_Bullet_List()
    {
        const string requirement = """
            Team Duration : 5 months

            Requested Roles:
              • Java Developer
              • SDET
              • Python Developer
              • DevOps Engineer
            """;

        var segments = AiTeamBuilderRequirementAnalyzer.SplitRoleSegments(requirement);

        Assert.Equal(4, segments.Count);
        Assert.Equal("Java Developer", segments[0]);
        Assert.Equal("SDET", segments[1]);
        Assert.Equal("Python Developer", segments[2]);
        Assert.Equal("DevOps Engineer", segments[3]);
    }

    [Fact]
    public void ParseRequirementToRoles_Creates_Role_Per_Bullet()
    {
        const string requirement = """
            Requested Roles:
              • SDET
              • Java Developer
            """;

        var roles = AiTeamBuilderRequirementAnalyzer.ParseRequirementToRoles(requirement);

        Assert.Equal(2, roles.Count);
        Assert.Equal("SDET", roles[0].RoleTitle);
        Assert.Equal("Java Developer", roles[1].RoleTitle);
    }
}
