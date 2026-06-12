using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prm.Application.Common;
using Prm.Application.DTOs.Admin;
using Prm.Application.Interfaces;
using Prm.Api.Controllers;

namespace Prm.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/projects")]
[Authorize(Policy = AuthConstants.PolicyNames.AdminOnly)]
public class AdminProjectsController : AuthenticatedControllerBase
{
    private readonly IAdminProjectService _service;

    public AdminProjectsController(IAdminProjectService service, ICurrentUserAccessor currentUserAccessor)
        : base(currentUserAccessor)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProjectListItemDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await _service.GetAllAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ProjectListItemDto>> Create([FromBody] CreateProjectRequest request, CancellationToken cancellationToken) =>
        Ok(await _service.CreateAsync(request, GetUserId(), cancellationToken));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProjectListItemDto>> Update(int id, [FromBody] UpdateProjectRequest request, CancellationToken cancellationToken) =>
        Ok(await _service.UpdateAsync(id, request, GetUserId(), cancellationToken));

    [HttpGet("{id:int}/milestones")]
    public async Task<ActionResult<IReadOnlyList<MilestoneDto>>> GetMilestones(int id, CancellationToken cancellationToken) =>
        Ok(await _service.GetMilestonesAsync(id, cancellationToken));

    [HttpPost("{id:int}/milestones")]
    public async Task<ActionResult<MilestoneDto>> AddMilestone(
        int id,
        [FromBody] AddMilestoneRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _service.AddMilestoneAsync(id, request, GetUserId(), cancellationToken));

    [HttpPut("{id:int}/milestones/{milestoneId:int}/status")]
    public async Task<ActionResult<MilestoneDto>> UpdateMilestoneStatus(
        int id,
        int milestoneId,
        [FromBody] UpdateMilestoneStatusRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _service.UpdateMilestoneStatusAsync(id, milestoneId, request, GetUserId(), cancellationToken));
}
