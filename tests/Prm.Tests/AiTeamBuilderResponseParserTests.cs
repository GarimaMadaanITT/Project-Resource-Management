using Prm.Application.Validation;
using Prm.Domain.Exceptions;

namespace Prm.Tests;

public class AiTeamBuilderResponseParserTests
{
    [Fact]
    public void Parse_Deserializes_Valid_Team_Builder_Json()
    {
        const string json = """
            {
              "roles": [
                {
                  "roleTitle": "Senior Java Developer",
                  "status": "FILLED",
                  "requiredSkills": [{"skillName":"Java","minProficiency":"ADVANCED"}],
                  "assignedEmployeeName": "Anil Mehta",
                  "matchScore": 90,
                  "reason": "Advanced Java on bench."
                },
                {
                  "roleTitle": "QA Tester",
                  "status": "GAP",
                  "requiredSkills": [{"skillName":"Selenium","minProficiency":"BEGINNER"}],
                  "gap": {
                    "reasonType": "NO_SKILL",
                    "message": "No employee has Selenium skills."
                  }
                }
              ]
            }
            """;

        var roles = AiTeamBuilderResponseParser.Parse(json);

        Assert.Equal(2, roles.Count);
        Assert.Equal("Senior Java Developer", roles[0].RoleTitle);
        Assert.Equal("FILLED", roles[0].Status);
        Assert.Equal("Anil Mehta", roles[0].AssignedEmployeeName);
        Assert.Equal("GAP", roles[1].Status);
        Assert.Equal("NO_SKILL", roles[1].Gap?.ReasonType);
    }

    [Fact]
    public void Parse_Throws_For_Malformed_Json()
    {
        Assert.Throws<DomainException>(() => AiTeamBuilderResponseParser.Parse("not json"));
    }
}
