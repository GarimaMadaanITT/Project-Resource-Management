namespace Prm.Application.Common;

public static class TeamBuilderConstants
{
    public const string StatusFilled = "FILLED";
    public const string StatusGap = "GAP";
    public const string GapReasonNoSkill = "NO_SKILL";
    public const string GapReasonAllocatedElsewhere = "ALLOCATED_ELSEWHERE";

    public const string TeamBuilderDisclaimer =
        "Note: AI-generated team suggestions. Only fully benched employees are assigned. No allocation performed.";

    public const string TeamBuilderSystemPromptKeyword = "team builder assistant";

    public const string ProficiencyAny = "ANY";
}
