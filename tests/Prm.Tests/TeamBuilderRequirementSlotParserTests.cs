using Prm.Application.Validation;

namespace Prm.Tests;

public class TeamBuilderRequirementSlotParserTests
{
    [Fact]
    public void ParseSlots_Expands_Counts_And_Normalizes_Typos()
    {
        var slots = TeamBuilderRequirementSlotParser.ParseSlots(
            "I want 2 sedts and 1 java develpoer and 1 devops for 5 months");

        Assert.Equal(4, slots.Count);
        Assert.Equal(2, slots.Count(slot => slot.RoleTitle == "SDET"));
        Assert.Single(slots, slot => slot.RoleTitle == "Java Developer");
        Assert.Single(slots, slot => slot.RoleTitle == "DevOps Engineer");
    }

    [Fact]
    public void ParseSlots_Treats_Implicit_A_As_One()
    {
        var slots = TeamBuilderRequirementSlotParser.ParseSlots(
            "i want a devops and 1 java developer for 5 months");

        Assert.Equal(2, slots.Count);
        Assert.Single(slots, slot => slot.RoleTitle == "DevOps Engineer");
        Assert.Single(slots, slot => slot.RoleTitle == "Java Developer");
    }
}
