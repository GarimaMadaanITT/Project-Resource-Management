using System.Text.RegularExpressions;
using Prm.Domain.Exceptions;

namespace Prm.Application.Validation;

public static partial class OrgLabelValidator
{
    public static string ValidateRequired(string? value, string fieldName)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new DomainException($"{fieldName} is required.");
        }

        return ValidateOptional(trimmed, fieldName)!;
    }

    public static string? ValidateOptional(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (!ContainsLetter(trimmed))
        {
            throw new DomainException($"{fieldName} must contain at least one letter.");
        }

        return trimmed;
    }

    private static bool ContainsLetter(string value) => LetterPattern().IsMatch(value);

    [GeneratedRegex(@"[a-zA-Z]")]
    private static partial Regex LetterPattern();
}
