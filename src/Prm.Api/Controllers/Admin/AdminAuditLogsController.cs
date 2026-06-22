using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prm.Application.Common;
using Prm.Application.DTOs.Admin;
using Prm.Application.Interfaces;
using Prm.Api.Controllers;

namespace Prm.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/audit-logs")]
[Authorize(Policy = AuthConstants.PolicyNames.AdminOnly)]
public class AdminAuditLogsController : ControllerBase
{
    private readonly IAdminAuditLogQueryService _service;

    public AdminAuditLogsController(IAdminAuditLogQueryService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<AuditLogListResponse>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = ValidationConstants.DefaultPageSize,
        [FromQuery] string? entityName = null,
        [FromQuery] string? action = null,
        [FromQuery] string? source = null,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        CancellationToken cancellationToken = default) =>
        Ok(await _service.QueryAsync(
            new AuditLogQuery(page, pageSize, entityName, action, source, from, to),
            cancellationToken));
}
