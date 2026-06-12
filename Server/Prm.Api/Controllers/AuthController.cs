using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prm.Application.Common;
using Prm.Application.DTOs.Auth;
using Prm.Application.Interfaces;

namespace Prm.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : AuthenticatedControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService, ICurrentUserAccessor currentUserAccessor)
        : base(currentUserAccessor)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken) =>
        Ok(await _authService.LoginAsync(request, cancellationToken));

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<ChangePasswordResponse>> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var response = await _authService.ChangePasswordAsync(userId, request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<AuthenticatedUserDto>> Me(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var user = await _authService.GetCurrentUserAsync(userId, cancellationToken);
        return Ok(user);
    }

    [HttpGet("admin-check")]
    [Authorize(Policy = AuthConstants.PolicyNames.AdminOnly)]
    public IActionResult AdminCheck() =>
        Ok(new { message = "Admin access granted.", role = AuthConstants.RoleName(Domain.Enums.UserRole.Admin) });

    [HttpGet("manager-check")]
    [Authorize(Policy = AuthConstants.PolicyNames.ManagerOnly)]
    public IActionResult ManagerCheck() =>
        Ok(new { message = "Manager access granted.", role = AuthConstants.RoleName(Domain.Enums.UserRole.Manager) });

    [HttpGet("employee-check")]
    [Authorize(Policy = AuthConstants.PolicyNames.EmployeeOnly)]
    public IActionResult EmployeeCheck() =>
        Ok(new { message = "Employee access granted.", role = AuthConstants.RoleName(Domain.Enums.UserRole.Employee) });
}
