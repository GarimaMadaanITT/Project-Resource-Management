using System.Text.Json;
using Microsoft.Extensions.Logging;
using Prm.Application.Interfaces;
using Prm.Domain.Entities;

namespace Prm.Application.Services.Shared;

public class AuditLogService : IAuditLogService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IAuditLogRepository _auditLogs;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(IAuditLogRepository auditLogs, ILogger<AuditLogService> logger)
    {
        _auditLogs = auditLogs;
        _logger = logger;
    }

    public async Task AuditAsync(
        string entityName,
        int entityId,
        string action,
        object? oldValue,
        object? newValue,
        int? performedByUserId,
        string? performedByRole,
        string source,
        CancellationToken cancellationToken = default)
    {
        var oldJson = Serialize(oldValue);
        var newJson = Serialize(newValue);

        if (string.Equals(oldJson, newJson, StringComparison.Ordinal))
        {
            return;
        }

        var entry = new AuditLog
        {
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            OldValue = oldJson,
            NewValue = newJson,
            PerformedByUserId = performedByUserId,
            PerformedByRole = performedByRole,
            Source = source,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _auditLogs.AddAsync(entry, cancellationToken);

        _logger.LogInformation(
            "Audit recorded. EntityName={EntityName}, EntityId={EntityId}, Action={Action}, Source={Source}, PerformedByUserId={PerformedByUserId}",
            entityName,
            entityId,
            action,
            source,
            performedByUserId);
    }

    private static string? Serialize(object? value) =>
        value is null ? null : JsonSerializer.Serialize(value, JsonOptions);
}
