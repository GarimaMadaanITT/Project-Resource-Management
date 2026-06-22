using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prm.Application.Common;
using Prm.Application.DTOs.Admin;
using Prm.Application.Interfaces;

namespace Prm.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/allocations")]
[Authorize(Policy = AuthConstants.PolicyNames.AdminOnly)]
public class AdminAllocationsController : ControllerBase
{
    private readonly IAdminAllocationService _service;

    public AdminAllocationsController(IAdminAllocationService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AllocationListItemDto>>> GetAll(
        [FromQuery] int? employeeId,
        [FromQuery] int? projectId,
        CancellationToken cancellationToken) =>
        Ok(await _service.GetAllAsync(employeeId, projectId, cancellationToken));
}
