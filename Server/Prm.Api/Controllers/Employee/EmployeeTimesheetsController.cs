using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prm.Application.Common;
using Prm.Application.DTOs.Employee;
using Prm.Application.Interfaces;

namespace Prm.Api.Controllers.Employee;

[ApiController]
[Route("api/employee/timesheets")]
[Authorize(Policy = AuthConstants.PolicyNames.EmployeeOnly)]
public class EmployeeTimesheetsController : AuthenticatedControllerBase
{
    private readonly IEmployeeTimesheetService _service;

    public EmployeeTimesheetsController(
        IEmployeeTimesheetService service,
        ICurrentUserAccessor currentUserAccessor)
        : base(currentUserAccessor)
    {
        _service = service;
    }

    [HttpGet("activity-tags")]
    public ActionResult<ActivityTagsResponse> GetActivityTags() =>
        Ok(_service.GetActivityTags());

    [HttpGet]
    public async Task<ActionResult<MyTimesheetsResponse>> GetMyTimesheets(CancellationToken cancellationToken) =>
        Ok(await _service.GetMyTimesheetsAsync(GetUserId(), cancellationToken));

    [HttpGet("{weekStart}")]
    public async Task<ActionResult<TimesheetWeekDetailDto>> GetWeekDetail(
        DateOnly weekStart,
        CancellationToken cancellationToken) =>
        Ok(await _service.GetWeekDetailAsync(GetUserId(), weekStart, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<SubmitTimesheetResponse>> Submit(
        [FromBody] SubmitTimesheetRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.SubmitAsync(GetUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(GetWeekDetail), new { weekStart = result.WeekStart }, result);
    }
}
