using System.Security.Claims;
using Prm.Application.Common;

namespace Prm.Application.Interfaces;

public interface ICurrentUserAccessor
{
    int GetUserId(ClaimsPrincipal user);
}

public class CurrentUserAccessor : ICurrentUserAccessor
{
    private const string SubjectClaimType = "sub";

    public int GetUserId(ClaimsPrincipal user)
    {
        var subject = user.FindFirst(SubjectClaimType)?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (subject is null || !int.TryParse(subject, out var userId))
        {
            throw new UnauthorizedAccessException(ErrorMessages.InvalidToken);
        }

        return userId;
    }
}
