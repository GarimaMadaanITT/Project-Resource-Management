using Prm.Domain.Exceptions;
using Prm.Application.Common;

namespace Prm.Application.Validation;

public static class UserGuard
{
    public static void EnsureNotSelfDeactivation(int actingUserId, int targetUserId)
    {
        if (actingUserId == targetUserId)
        {
            throw new ForbiddenException("You cannot deactivate your own account.");
        }
    }

    public static void EnsureNotLastActiveAdmin(int activeAdminCount)
    {
        if (activeAdminCount <= ValidationConstants.MinActiveAdminCount)
        {
            throw new ForbiddenException("Cannot deactivate the last active Admin account.");
        }
    }
}
