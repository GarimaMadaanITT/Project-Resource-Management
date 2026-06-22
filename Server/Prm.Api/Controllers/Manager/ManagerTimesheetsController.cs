using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prm.Application.Common;
using Prm.Application.DTOs.Manager;
using Prm.Application.Interfaces;

namespace Prm.Api.Controllers.Manager;

[ApiController]
[Route("api/manager/timesheets")]
[Authorize(Policy = AuthConstants.PolicyNames.ManagerOnly)]
public class ManagerTimesheetsController : AuthenticatedControllerBase
{
    private readonly IManagerTeamTimesheetService _service;
    private readonly IManagerTimesheetComplianceService _complianceService;

    public ManagerTimesheetsController(
        IManagerTeamTimesheetService service,
        IManagerTimesheetComplianceService complianceService,
        ICurrentUserAccessor currentUserAccessor)
        : base(currentUserAccessor)
    {
        _service = service;
        _complianceService = complianceService;
    }

    [HttpGet]
    public async Task<ActionResult<TeamTimesheetsResponse>> GetTeamTimesheets(
        [FromQuery] DateOnly? weekStart,
        CancellationToken cancellationToken) =>
        Ok(await _service.GetTeamTimesheetsAsync(GetUserId(), weekStart, cancellationToken));

    [HttpGet("employees/{employeeId:int}")]
    public async Task<ActionResult<ManagerEmployeeTimesheetDetailResponse>> GetEmployeeTimesheetDetail(
        int employeeId,
        [FromQuery] DateOnly? weekStart,
        CancellationToken cancellationToken) =>
        Ok(await _service.GetEmployeeTimesheetDetailAsync(GetUserId(), employeeId, weekStart, cancellationToken));

    [HttpPost("employees/{employeeId:int}/restore-timesheet-access")]
    public async Task<ActionResult<RestoreTimesheetAccessResponse>> RestoreTimesheetAccess(
        int employeeId,
        CancellationToken cancellationToken) =>
        Ok(await _complianceService.RestoreTimesheetAccessAsync(GetUserId(), employeeId, cancellationToken));
}
