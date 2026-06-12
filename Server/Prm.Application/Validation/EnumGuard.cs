using Prm.Domain.Exceptions;

namespace Prm.Application.Validation;

public static class EnumGuard
{
    public static T Parse<T>(string value, string fieldName) where T : struct, Enum
    {
        if (!Enum.TryParse<T>(value, true, out var parsed))
        {
            throw new DomainException($"Invalid {fieldName}: {value}.");
        }

        return parsed;
    }
}
