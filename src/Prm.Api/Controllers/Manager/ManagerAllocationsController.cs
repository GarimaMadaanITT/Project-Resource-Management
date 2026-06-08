using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prm.Application.Common;
using Prm.Application.DTOs.Manager;
using Prm.Application.Interfaces;

namespace Prm.Api.Controllers.Manager;

[ApiController]
[Route("api/manager/allocations")]
[Authorize(Policy = AuthConstants.PolicyNames.ManagerOnly)]
public class ManagerAllocationsController : AuthenticatedControllerBase
{
    private readonly IManagerAllocationService _service;

    public ManagerAllocationsController(IManagerAllocationService service, ICurrentUserAccessor currentUserAccessor)
        : base(currentUserAccessor)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<ManagerAllocationDto>> Create(
        [FromBody] CreateManagerAllocationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(GetUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
    }

    [HttpPost("{id:int}/end")]
    public async Task<ActionResult<EndAllocationResponse>> End(
        int id,
        [FromBody] EndAllocationRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _service.EndAsync(GetUserId(), id, request, cancellationToken));
}
