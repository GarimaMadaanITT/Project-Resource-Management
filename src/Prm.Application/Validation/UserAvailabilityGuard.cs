using Prm.Application.Common;
using Prm.Domain.Exceptions;

namespace Prm.Application.Validation;

public static class UserAvailabilityGuard
{
    public static void EnsureUsernameAvailable(bool exists)
    {
        if (exists)
        {
            throw new ConflictException(ErrorMessages.UsernameAlreadyExists);
        }
    }

    public static void EnsureEmailAvailable(bool exists)
    {
        if (exists)
        {
            throw new ConflictException(ErrorMessages.EmailAlreadyExists);
        }
    }

    public static void EnsureNotAlreadyInactive(bool isActive)
    {
        if (!isActive)
        {
            throw new DomainException(ErrorMessages.UserAlreadyInactive);
        }
    }
}
