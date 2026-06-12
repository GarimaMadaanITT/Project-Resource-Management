namespace Prm.Application.Interfaces;

public interface IEmployeeContextService
{
    Task<EmployeeContext> ResolveAsync(int userId, CancellationToken cancellationToken = default);
}

public record EmployeeContext(int UserId, int ResourceProfileId, string EmployeeName);
