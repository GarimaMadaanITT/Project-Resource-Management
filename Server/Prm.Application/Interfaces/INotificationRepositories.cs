using Prm.Domain.Entities;
using Prm.Domain.Enums;

namespace Prm.Application.Interfaces;

public interface ITimesheetComplianceRepository
{
    Task<TimesheetCompliance?> GetByResourceProfileAndWeekAsync(
        int resourceProfileId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TimesheetCompliance>> GetByResourceProfileIdsAndWeeksAsync(
        IReadOnlyList<int> resourceProfileIds,
        IReadOnlyList<DateOnly> weekStarts,
        CancellationToken cancellationToken = default);

    Task AddAsync(TimesheetCompliance compliance, CancellationToken cancellationToken = default);

    Task UpdateAsync(TimesheetCompliance compliance, CancellationToken cancellationToken = default);

    Task DeleteAsync(TimesheetCompliance compliance, CancellationToken cancellationToken = default);
}

public interface INotificationLogRepository
{
    Task<bool> ExistsAsync(
        NotificationType notificationType,
        string referenceKey,
        CancellationToken cancellationToken = default);

    Task AddAsync(NotificationLog log, CancellationToken cancellationToken = default);
}
