namespace Prm.Application.Interfaces;

public interface IDatabaseHealthService
{
    Task<DatabaseHealthStatus> GetStatusAsync(CancellationToken cancellationToken = default);
}

public sealed record DatabaseHealthStatus(
    bool Connected,
    string Provider,
    int UserCount,
    int EmployeeCount,
    int ProjectCount,
    bool BootstrapAdminExists,
    string? ErrorMessage = null);
