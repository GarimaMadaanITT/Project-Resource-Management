using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prm.Application.Common;
using Prm.Application.DTOs.Admin;
using Prm.Application.Interfaces;

namespace Prm.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/settings")]
[Authorize(Policy = AuthConstants.PolicyNames.AdminOnly)]
public class AdminSettingsController : ControllerBase
{
    private readonly IAdminSettingsService _service;

    public AdminSettingsController(IAdminSettingsService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<SystemSettingsDto>> Get(CancellationToken cancellationToken) =>
        Ok(await _service.GetAsync(cancellationToken));

    [HttpPut]
    public async Task<ActionResult<SystemSettingsDto>> Update(
        [FromBody] UpdateSystemSettingsRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _service.UpdateAsync(request, cancellationToken));
}
