using Prm.Application.Interfaces;
using Prm.Domain.Entities;
using Prm.Domain.Enums;
using Prm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Prm.Infrastructure.Repositories;

public class TimesheetComplianceRepository : ITimesheetComplianceRepository
{
    private readonly PrmDbContext _context;

    public TimesheetComplianceRepository(PrmDbContext context)
    {
        _context = context;
    }

    public Task<TimesheetCompliance?> GetByResourceProfileAndWeekAsync(
        int resourceProfileId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default) =>
        _context.TimesheetCompliances
            .FirstOrDefaultAsync(
                compliance => compliance.ResourceProfileId == resourceProfileId && compliance.WeekStart == weekStart,
                cancellationToken);

    public async Task<IReadOnlyList<TimesheetCompliance>> GetByResourceProfileIdsAndWeeksAsync(
        IReadOnlyList<int> resourceProfileIds,
        IReadOnlyList<DateOnly> weekStarts,
        CancellationToken cancellationToken = default)
    {
        if (resourceProfileIds.Count == 0 || weekStarts.Count == 0)
        {
            return Array.Empty<TimesheetCompliance>();
        }

        return await _context.TimesheetCompliances
            .AsNoTracking()
            .Where(compliance =>
                resourceProfileIds.Contains(compliance.ResourceProfileId)
                && weekStarts.Contains(compliance.WeekStart))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TimesheetCompliance compliance, CancellationToken cancellationToken = default)
    {
        _context.TimesheetCompliances.Add(compliance);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TimesheetCompliance compliance, CancellationToken cancellationToken = default)
    {
        compliance.UpdatedAt = DateTime.UtcNow;
        _context.TimesheetCompliances.Update(compliance);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(TimesheetCompliance compliance, CancellationToken cancellationToken = default)
    {
        _context.TimesheetCompliances.Remove(compliance);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class NotificationLogRepository : INotificationLogRepository
{
    private readonly PrmDbContext _context;

    public NotificationLogRepository(PrmDbContext context)
    {
        _context = context;
    }

    public Task<bool> ExistsAsync(
        NotificationType notificationType,
        string referenceKey,
        CancellationToken cancellationToken = default) =>
        _context.NotificationLogs.AnyAsync(
            log => log.NotificationType == notificationType && log.ReferenceKey == referenceKey,
            cancellationToken);

    public async Task AddAsync(NotificationLog log, CancellationToken cancellationToken = default)
    {
        _context.NotificationLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
