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

    public static bool DateRangesOverlap(DateOnly fromA, DateOnly toA, DateOnly fromB, DateOnly toB) =>
        fromA <= toB && fromB <= toA;

    public static bool IsAllocationActiveOnDate(Allocation allocation, DateOnly date) =>
        allocation.FromDate <= date && allocation.ToDate >= date;

    public static bool IsAllocationActiveDuringWeek(Allocation allocation, DateOnly weekStart)
    {
        var weekEnd = weekStart.AddDays(6);
        return DateRangesOverlap(allocation.FromDate, allocation.ToDate, weekStart, weekEnd);
    }

    public static DateOnly GetCurrentWeekStartUtc()
    {
        var today = TodayUtc;
        var daysFromMonday = ((int)today.DayOfWeek + 6) % 7;
        return today.AddDays(-daysFromMonday);
    }

    public static DateOnly ResolveWeekStart(DateOnly? weekStart)
    {
        var resolved = weekStart ?? GetCurrentWeekStartUtc();
        EnsureMondayWeekStart(resolved);
        return resolved;
    }

    public static void EnsureMondayWeekStart(DateOnly weekStart)
    {
        if (weekStart.DayOfWeek != DayOfWeek.Monday)
        {
            throw new Domain.Exceptions.DomainException("Week start must be a Monday.");
        }
    }

    public static DateOnly GetPreviousCompletedWeekStart() =>
        GetCurrentWeekStartUtc().AddDays(-7);

    public static int SumUtilisationOnDate(IEnumerable<Allocation> allocations, DateOnly date) =>
        allocations.Where(allocation => IsAllocationActiveOnDate(allocation, date))
            .Sum(allocation => allocation.UtilisationPercent);

    public static IEnumerable<DateOnly> GetCriticalDatesInRange(
        DateOnly rangeFrom,
        DateOnly rangeTo,
        IEnumerable<Allocation> existingAllocations)
    {
        var dates = new HashSet<DateOnly> { rangeFrom, rangeTo };

        foreach (var allocation in existingAllocations)
        {
            if (!DateRangesOverlap(allocation.FromDate, allocation.ToDate, rangeFrom, rangeTo))
            {
                continue;
            }

            if (allocation.FromDate >= rangeFrom && allocation.FromDate <= rangeTo)
            {
                dates.Add(allocation.FromDate);
            }

            if (allocation.ToDate >= rangeFrom && allocation.ToDate <= rangeTo)
            {
                dates.Add(allocation.ToDate);
            }
        }

        return dates.Where(date => date >= rangeFrom && date <= rangeTo).OrderBy(date => date);
    }
}
