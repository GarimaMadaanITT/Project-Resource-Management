namespace Prm.Application.Interfaces;

public interface IAuditLogService
{
    Task AuditAsync(
        string entityName,
        int entityId,
        string action,
        object? oldValue,
        object? newValue,
        int? performedByUserId,
        string? performedByRole,
        string source,
        CancellationToken cancellationToken = default);
}
