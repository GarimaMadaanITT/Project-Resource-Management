using System.Text.RegularExpressions;

namespace Prm.Application.Validation;

public static partial class AiRequirementParser
{
    public record ParsedRequirement(int? HoursPerWeek);

    [GeneratedRegex(@"(?<hours>\d+)\s*(?:hrs?|hours?)(?:\s*/\s*week|\s*per\s*week|\s*a\s*week)?", RegexOptions.IgnoreCase)]
    private static partial Regex HoursPerWeekPattern();

    public static ParsedRequirement Parse(string requirement)
    {
        StringGuard.RequireNonEmpty(requirement, "Requirement");

        var match = HoursPerWeekPattern().Match(requirement);
        if (!match.Success)
        {
            return new ParsedRequirement(null);
        }

        if (!int.TryParse(match.Groups["hours"].Value, out var hours) || hours <= 0)
        {
            return new ParsedRequirement(null);
        }

        return new ParsedRequirement(hours);
    }
}
