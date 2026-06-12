using Prm.Application.Common;
using Prm.Domain.Exceptions;

namespace Prm.Application.Validation;

public static class PasswordGuard
{
    public static void EnsureValid(string password)
    {
        if (!PasswordValidator.IsValid(password, out var errorMessage))
        {
            throw new DomainException(errorMessage);
        }
    }
}
