using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prm.Application.Common;
using Prm.Application.DTOs.Manager;
using Prm.Application.Interfaces;

namespace Prm.Api.Controllers.Manager;

[ApiController]
[Route("api/manager/dashboard")]
[Authorize(Policy = AuthConstants.PolicyNames.ManagerOnly)]
public class ManagerDashboardController : AuthenticatedControllerBase
{
    private readonly IManagerDashboardService _service;

    public ManagerDashboardController(IManagerDashboardService service, ICurrentUserAccessor currentUserAccessor)
        : base(currentUserAccessor)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<ManagerDashboardResponse>> GetDashboard(CancellationToken cancellationToken) =>
        Ok(await _service.GetDashboardAsync(GetUserId(), cancellationToken));

    [HttpGet("employees/{employeeId:int}")]
    public async Task<ActionResult<ManagerEmployeeDetailDto>> GetEmployeeDetail(
        int employeeId,
        CancellationToken cancellationToken) =>
        Ok(await _service.GetEmployeeDetailAsync(GetUserId(), employeeId, cancellationToken));
}
