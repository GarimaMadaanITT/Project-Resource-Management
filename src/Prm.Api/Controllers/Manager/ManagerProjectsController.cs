using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prm.Application.Common;
using Prm.Application.DTOs.Manager;
using Prm.Application.Interfaces;

namespace Prm.Api.Controllers.Manager;

[ApiController]
[Route("api/manager/projects")]
[Authorize(Policy = AuthConstants.PolicyNames.ManagerOnly)]
public class ManagerProjectsController : AuthenticatedControllerBase
{
    private readonly IManagerProjectService _service;

    public ManagerProjectsController(IManagerProjectService service, ICurrentUserAccessor currentUserAccessor)
        : base(currentUserAccessor)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ManagerProjectListItemDto>>> GetProjects(
        CancellationToken cancellationToken) =>
        Ok(await _service.GetProjectsAsync(GetUserId(), cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ManagerProjectDetailDto>> GetProjectDetail(
        int id,
        CancellationToken cancellationToken) =>
        Ok(await _service.GetProjectDetailAsync(GetUserId(), id, cancellationToken));
}
