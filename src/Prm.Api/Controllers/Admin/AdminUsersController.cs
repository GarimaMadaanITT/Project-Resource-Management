using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prm.Application.Common;
using Prm.Application.DTOs.Admin;
using Prm.Application.Interfaces;
using Prm.Api.Controllers;

namespace Prm.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = AuthConstants.PolicyNames.AdminOnly)]
public class AdminUsersController : AuthenticatedControllerBase
{
    private readonly IAdminUserService _service;

    public AdminUsersController(IAdminUserService service, ICurrentUserAccessor currentUserAccessor)
        : base(currentUserAccessor)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<UserListItemDto>> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken) =>
        Ok(await _service.CreateAsync(request, cancellationToken));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserListItemDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await _service.GetAllAsync(cancellationToken));

    [HttpPost("{id:int}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await _service.ResetPasswordAsync(id, request, cancellationToken);
        return Ok(new { message = "Password reset. User must change password on next login." });
    }

    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await _service.DeactivateAsync(id, GetUserId(), cancellationToken);
        return Ok(new { message = "User deactivated." });
    }

    [HttpPost("{id:int}/reactivate")]
    public async Task<IActionResult> Reactivate(int id, CancellationToken cancellationToken)
    {
        await _service.ReactivateAsync(id, cancellationToken);
        return Ok(new { message = "User reactivated." });
    }
}
