using Prm.Domain.Entities;

namespace Prm.Application.Validation;

public static class ActiveDateHelper
{
    public static DateOnly TodayUtc => DateOnly.FromDateTime(DateTime.UtcNow);

    public static bool IsAllocationActive(Allocation allocation, DateOnly today) =>
        allocation.FromDate <= today && allocation.ToDate >= today;

    public static int SumActiveUtilisation(IEnumerable<Allocation> allocations, DateOnly today) =>
        allocations.Where(allocation => IsAllocationActive(allocation, today))
            .Sum(allocation => allocation.UtilisationPercent);
}
