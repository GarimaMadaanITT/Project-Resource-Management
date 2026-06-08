using Microsoft.AspNetCore.Mvc;
using Prm.Application.Interfaces;

namespace Prm.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DatabaseController : ControllerBase
{
    private readonly IDatabaseHealthService _databaseHealthService;

    public DatabaseController(IDatabaseHealthService databaseHealthService)
    {
        _databaseHealthService = databaseHealthService;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var status = await _databaseHealthService.GetStatusAsync(cancellationToken);
        if (!status.Connected)
        {
            return StatusCode(503, new { connected = false, message = status.ErrorMessage });
        }

        return Ok(new
        {
            connected = true,
            provider = status.Provider,
            users = status.UserCount,
            employees = status.EmployeeCount,
            projects = status.ProjectCount,
            bootstrapAdmin = status.BootstrapAdminExists
        });
    }
}
