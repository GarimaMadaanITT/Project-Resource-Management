using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prm.Application.Common;
using Prm.Application.DTOs.Employee;
using Prm.Application.Interfaces;

namespace Prm.Api.Controllers.Employee;

[ApiController]
[Route("api/employee/reminders")]
[Authorize(Policy = AuthConstants.PolicyNames.EmployeeOnly)]
public class EmployeeRemindersController : AuthenticatedControllerBase
{
    private readonly IEmployeeReminderService _service;

    public EmployeeRemindersController(
        IEmployeeReminderService service,
        ICurrentUserAccessor currentUserAccessor)
        : base(currentUserAccessor)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<EmployeeReminderResponse>> GetReminder(CancellationToken cancellationToken) =>
        Ok(await _service.GetReminderAsync(GetUserId(), cancellationToken));
}
