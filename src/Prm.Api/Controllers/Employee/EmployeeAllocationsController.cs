using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prm.Application.Common;
using Prm.Application.DTOs.Employee;
using Prm.Application.Interfaces;

namespace Prm.Api.Controllers.Employee;

[ApiController]
[Route("api/employee/allocations")]
[Authorize(Policy = AuthConstants.PolicyNames.EmployeeOnly)]
public class EmployeeAllocationsController : AuthenticatedControllerBase
{
    private readonly IEmployeeAllocationService _service;

    public EmployeeAllocationsController(
        IEmployeeAllocationService service,
        ICurrentUserAccessor currentUserAccessor)
        : base(currentUserAccessor)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<MyAllocationsResponse>> GetMyAllocations(CancellationToken cancellationToken) =>
        Ok(await _service.GetMyAllocationsAsync(GetUserId(), cancellationToken));
}
