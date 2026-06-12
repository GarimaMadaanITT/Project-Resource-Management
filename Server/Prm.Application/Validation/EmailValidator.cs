using System.Net.Mail;
using Prm.Domain.Exceptions;

namespace Prm.Application.Validation;

public static class EmailValidator
{
    public static string ValidateAndNormalize(string email)
    {
        var normalized = StringGuard.RequireNonEmpty(email, "Email");

        try
        {
            _ = new MailAddress(normalized);
        }
        catch (FormatException)
        {
            throw new DomainException("Email format is invalid.");
        }

        return normalized;
    }
}
