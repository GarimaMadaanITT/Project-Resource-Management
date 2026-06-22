using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prm.Application.Common;
using Prm.Application.DTOs.Manager;
using Prm.Application.Interfaces;

namespace Prm.Api.Controllers.Manager;

[ApiController]
[Route("api/ai")]
[Authorize(Policy = AuthConstants.PolicyNames.ManagerOnly)]
public class ManagerAiController : AuthenticatedControllerBase
{
    private readonly IManagerAiService _service;

    public ManagerAiController(IManagerAiService service, ICurrentUserAccessor currentUserAccessor)
        : base(currentUserAccessor)
    {
        _service = service;
    }

    [HttpPost("skill-match")]
    public async Task<ActionResult<SkillMatchResponse>> SkillMatch(
        [FromBody] SkillMatchRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _service.SkillMatchAsync(GetUserId(), request, cancellationToken));

    [HttpGet("risk-summary/{projectId:int}")]
    public async Task<ActionResult<RiskSummaryResponse>> RiskSummary(
        int projectId,
        CancellationToken cancellationToken) =>
        Ok(await _service.GetRiskSummaryAsync(GetUserId(), projectId, cancellationToken));

    [HttpPost("team-builder")]
    public async Task<ActionResult<TeamBuilderResponse>> TeamBuilder(
        [FromBody] TeamBuilderRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _service.TeamBuilderAsync(GetUserId(), request, cancellationToken));
}
