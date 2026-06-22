using Microsoft.EntityFrameworkCore;
using Prm.Application.DTOs.Admin;
using Prm.Application.Interfaces;
using Prm.Domain.Entities;
using Prm.Infrastructure.Persistence;

namespace Prm.Infrastructure.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly PrmDbContext _context;

    public AuditLogRepository(PrmDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AuditLog log, CancellationToken cancellationToken = default)
    {
        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> QueryAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken = default)
    {
        var auditQuery = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.EntityName))
        {
            auditQuery = auditQuery.Where(log => log.EntityName == query.EntityName);
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            auditQuery = auditQuery.Where(log => log.Action == query.Action);
        }

        if (!string.IsNullOrWhiteSpace(query.Source))
        {
            auditQuery = auditQuery.Where(log => log.Source == query.Source);
        }

        if (query.From.HasValue)
        {
            var fromUtc = query.From.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            auditQuery = auditQuery.Where(log => log.CreatedAtUtc >= fromUtc);
        }

        if (query.To.HasValue)
        {
            var toUtc = query.To.Value.ToDateTime(new TimeOnly(23, 59, 59), DateTimeKind.Utc);
            auditQuery = auditQuery.Where(log => log.CreatedAtUtc <= toUtc);
        }

        var totalCount = await auditQuery.CountAsync(cancellationToken);

        var items = await auditQuery
            .OrderByDescending(log => log.CreatedAtUtc)
            .ThenByDescending(log => log.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
