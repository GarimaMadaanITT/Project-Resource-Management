using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prm.Application.Common;
using Prm.Application.DTOs.Admin;
using Prm.Application.Interfaces;
using Prm.Api.Controllers;

namespace Prm.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/employees")]
[Authorize(Policy = AuthConstants.PolicyNames.AdminOnly)]
public class AdminEmployeesController : AuthenticatedControllerBase
{
    private readonly IAdminEmployeeService _service;

    public AdminEmployeesController(IAdminEmployeeService service, ICurrentUserAccessor currentUserAccessor)
        : base(currentUserAccessor)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<EmployeeListResponse>> GetAll(
        [FromQuery] string? department,
        [FromQuery] string? status,
        CancellationToken cancellationToken) =>
        Ok(await _service.GetAllAsync(department, status, cancellationToken));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEmployeeRequest request, CancellationToken cancellationToken)
    {
        await _service.UpdateAsync(id, request, cancellationToken);
        return Ok(new { message = "Employee updated." });
    }

    [HttpPost("{id:int}/deactivate")]
    public async Task<ActionResult<DeactivateEmployeeResponse>> Deactivate(int id, CancellationToken cancellationToken) =>
        Ok(await _service.DeactivateAsync(id, GetUserId(), cancellationToken));

    [HttpGet("{id:int}/skills")]
    public async Task<ActionResult<IReadOnlyList<EmployeeSkillDto>>> GetSkills(int id, CancellationToken cancellationToken) =>
        Ok(await _service.GetSkillsAsync(id, cancellationToken));

    [HttpPost("{id:int}/skills")]
    public async Task<ActionResult<EmployeeSkillDto>> AddSkill(
        int id,
        [FromBody] AddEmployeeSkillRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _service.AddSkillAsync(id, request, cancellationToken));

    [HttpPut("{id:int}/skills/{skillId:int}")]
    public async Task<ActionResult<EmployeeSkillDto>> UpdateSkill(
        int id,
        int skillId,
        [FromBody] UpdateEmployeeSkillRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _service.UpdateSkillAsync(id, skillId, request, cancellationToken));

    [HttpDelete("{id:int}/skills/{skillId:int}")]
    public async Task<IActionResult> RemoveSkill(int id, int skillId, CancellationToken cancellationToken)
    {
        await _service.RemoveSkillAsync(id, skillId, cancellationToken);
        return Ok(new { message = "Skill removed." });
    }

    [HttpPut("{id:int}/assign-manager")]
    public async Task<IActionResult> AssignManager(int id, [FromBody] AssignManagerRequest request, CancellationToken cancellationToken)
    {
        await _service.AssignManagerAsync(id, request, cancellationToken);
        return Ok(new { message = "Manager assigned." });
    }
}
