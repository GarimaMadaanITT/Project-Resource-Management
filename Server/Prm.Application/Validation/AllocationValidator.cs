using Microsoft.Extensions.Logging;
using Prm.Application.Common;
using Prm.Domain.Entities;
using Prm.Domain.Enums;
using Prm.Domain.Exceptions;

namespace Prm.Application.Validation;

public static class AllocationValidator
{
    public static void ValidateCreateRequest(
        Project project,
        ResourceProfile resourceProfile,
        int utilisationPercent,
        DateOnly fromDate,
        DateOnly toDate,
        IReadOnlyList<Allocation> existingAllocations,
        ILogger? logger = null)
    {
        ValidateDateRange(fromDate, toDate);
        ValidateUtilisationPercent(utilisationPercent);
        ValidateProjectStatus(project);
        AllocationGuard.EnsureEmployeeCanReceiveAllocation(resourceProfile);
        ValidateOverlapCapacity(existingAllocations, utilisationPercent, fromDate, toDate, logger);
    }

    public static void ValidateEndDate(Allocation allocation, DateOnly endDate)
    {
        if (endDate < allocation.FromDate)
        {
            throw new DomainException("End date cannot be before the allocation start date.");
        }

        if (endDate > allocation.ToDate)
        {
            throw new DomainException("End date cannot be after the current allocation end date.");
        }
    }

    private static void ValidateDateRange(DateOnly fromDate, DateOnly toDate)
    {
        if (fromDate >= toDate)
        {
            throw new DomainException("From date must be before to date.");
        }
    }

    private static void ValidateUtilisationPercent(int utilisationPercent)
    {
        if (utilisationPercent < ValidationConstants.MinUtilisationPercent
            || utilisationPercent > ValidationConstants.MaxUtilisationPercent)
        {
            throw new DomainException(
                $"Utilisation must be between {ValidationConstants.MinUtilisationPercent} and {ValidationConstants.MaxUtilisationPercent}.");
        }
    }

    private static void ValidateProjectStatus(Project project)
    {
        if (project.Status is not (ProjectStatus.Active or ProjectStatus.Planned))
        {
            throw new DomainException("Allocations can only be created for projects in Active or Planned status.");
        }
    }

    private static void ValidateOverlapCapacity(
        IReadOnlyList<Allocation> existingAllocations,
        int newUtilisationPercent,
        DateOnly fromDate,
        DateOnly toDate,
        ILogger? logger)
    {
        var criticalDates = ActiveDateHelper.GetCriticalDatesInRange(fromDate, toDate, existingAllocations);

        foreach (var date in criticalDates)
        {
            var existingTotal = ActiveDateHelper.SumUtilisationOnDate(existingAllocations, date);
            var combinedTotal = existingTotal + newUtilisationPercent;

            if (combinedTotal > ValidationConstants.MaxUtilisationPercent)
            {
                logger?.LogWarning(
                    "Allocation rejected. ResourceProfileId={ResourceProfileId}, Date={Date}, ExistingUtilisation={Existing}, RequestedUtilisation={Requested}",
                    existingAllocations.FirstOrDefault()?.ResourceProfileId,
                    date,
                    existingTotal,
                    newUtilisationPercent);

                throw new DomainException(
                    $"Total utilisation would exceed {ValidationConstants.MaxUtilisationPercent}% on {date:yyyy-MM-dd}. " +
                    $"Existing: {existingTotal}%, requested: {newUtilisationPercent}%.");
            }
        }
    }
}
