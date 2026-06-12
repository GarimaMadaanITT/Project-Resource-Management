using Microsoft.AspNetCore.Mvc;
using Prm.Application.Interfaces;

namespace Prm.Api.Controllers;

public abstract class AuthenticatedControllerBase : ControllerBase
{
    private readonly ICurrentUserAccessor _currentUserAccessor;

    protected AuthenticatedControllerBase(ICurrentUserAccessor currentUserAccessor)
    {
        _currentUserAccessor = currentUserAccessor;
    }

    protected int GetUserId() => _currentUserAccessor.GetUserId(User);
}
