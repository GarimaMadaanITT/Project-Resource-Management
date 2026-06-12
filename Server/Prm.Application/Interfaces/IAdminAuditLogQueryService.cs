using Prm.Application.DTOs.Admin;

namespace Prm.Application.Interfaces;

public interface IAdminAuditLogQueryService
{
    Task<AuditLogListResponse> QueryAsync(AuditLogQuery query, CancellationToken cancellationToken = default);
}
