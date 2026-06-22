using Prm.Domain.Exceptions;

namespace Prm.Application.Validation;

public static class StringGuard
{
    public static string RequireNonEmpty(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} is required.");
        }

        return value.Trim();
    }
}
