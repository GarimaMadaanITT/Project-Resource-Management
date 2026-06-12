using Prm.Application.Common;
using Prm.Application.DTOs.Admin;
using Prm.Application.Interfaces;

namespace Prm.Application.Services.Admin;

public class AdminAuditLogQueryService : IAdminAuditLogQueryService
{
    private readonly IAuditLogRepository _auditLogs;

    public AdminAuditLogQueryService(IAuditLogRepository auditLogs)
    {
        _auditLogs = auditLogs;
    }

    public async Task<AuditLogListResponse> QueryAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize switch
        {
            < 1 => ValidationConstants.DefaultPageSize,
            > ValidationConstants.MaxPageSize => ValidationConstants.MaxPageSize,
            _ => query.PageSize
        };

        var normalizedQuery = query with { Page = page, PageSize = pageSize };
        var (items, totalCount) = await _auditLogs.QueryAsync(normalizedQuery, cancellationToken);

        var dtos = items
            .Select(item => new AuditLogItemDto(
                item.Id,
                item.EntityName,
                item.EntityId,
                item.Action,
                item.OldValue,
                item.NewValue,
                item.PerformedByUserId,
                item.PerformedByRole,
                item.Source,
                item.CreatedAtUtc))
            .ToList();

        return new AuditLogListResponse(dtos, page, pageSize, totalCount);
    }
}
