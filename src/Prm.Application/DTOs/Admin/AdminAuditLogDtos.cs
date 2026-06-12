namespace Prm.Application.DTOs.Admin;

public record AuditLogQuery(
    int Page,
    int PageSize,
    string? EntityName,
    string? Action,
    string? Source,
    DateOnly? From,
    DateOnly? To);

public record AuditLogItemDto(
    int Id,
    string EntityName,
    int EntityId,
    string Action,
    string? OldValue,
    string? NewValue,
    int? PerformedByUserId,
    string? PerformedByRole,
    string Source,
    DateTime CreatedAtUtc);

public record AuditLogListResponse(
    IReadOnlyList<AuditLogItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
