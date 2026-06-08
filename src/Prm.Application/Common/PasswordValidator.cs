using System.Text.RegularExpressions;

namespace Prm.Application.Common;

public static partial class PasswordValidator
{
    public static bool IsValid(string password, out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            errorMessage = "Password is required.";
            return false;
        }

        if (password.Length < ValidationConstants.MinPasswordLength)
        {
            errorMessage = $"Password must be at least {ValidationConstants.MinPasswordLength} characters.";
            return false;
        }

        if (!UppercaseRegex().IsMatch(password))
        {
            errorMessage = DigitRegex().IsMatch(password)
                ? "Password must contain at least one letter (uppercase) and one number."
                : "Password must contain at least one uppercase letter.";
            return false;
        }

        if (!DigitRegex().IsMatch(password))
        {
            errorMessage = "Password must contain at least one number.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    [GeneratedRegex("[A-Z]")]
    private static partial Regex UppercaseRegex();

    [GeneratedRegex("[0-9]")]
    private static partial Regex DigitRegex();
}
