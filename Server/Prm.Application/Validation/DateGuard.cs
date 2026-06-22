using Prm.Domain.Exceptions;

namespace Prm.Application.Validation;

public static class DateGuard
{
    public static void EnsureNotInPast(DateOnly date, string fieldName)
    {
        if (date < ActiveDateHelper.TodayUtc)
        {
            throw new DomainException($"{fieldName} cannot be in the past.");
        }
    }
}
