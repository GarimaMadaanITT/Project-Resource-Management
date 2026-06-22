using Microsoft.Extensions.Logging;
using Prm.Application.Common;
using Prm.Application.DTOs.Employee;
using Prm.Domain.Entities;
using Prm.Domain.Exceptions;

namespace Prm.Application.Validation;

public static class TimesheetValidator
{
    public static void ValidateSubmitRequest(
        SubmitTimesheetRequest request,
        DateOnly weekStart,
        IReadOnlyList<Allocation> weekAllocations,
        int maxWeeklyHours,
        bool timesheetAlreadyExists,
        ILogger? logger = null)
    {
        if (timesheetAlreadyExists)
        {
            throw new ConflictException(ErrorMessages.TimesheetAlreadySubmitted);
        }

        ValidateWeekStart(weekStart, logger);

        if (weekAllocations.Count == 0)
        {
            throw new DomainException("You have no project allocations for this week.");
        }

        if (request.Entries is null || request.Entries.Count == 0)
        {
            throw new DomainException("At least one timesheet entry is required.");
        }

        ValidateUniqueProjectIds(request.Entries);

        var allocationByProject = weekAllocations.ToDictionary(allocation => allocation.ProjectId);
        decimal totalHours = 0;

        foreach (var entry in request.Entries)
        {
            if (!allocationByProject.TryGetValue(entry.ProjectId, out var allocation))
            {
                logger?.LogWarning(
                    "Timesheet validation rejected. WeekStart={WeekStart}, ProjectId={ProjectId}, Reason=NotAllocated",
                    weekStart,
                    entry.ProjectId);

                throw new DomainException("You can only log hours for projects you are allocated to during this week.");
            }

            if (entry.Hours < 0)
            {
                throw new DomainException("Hours cannot be negative.");
            }

            var projectMaxHours = CalculateProjectMaxHours(allocation.UtilisationPercent, maxWeeklyHours);
            if (entry.Hours > projectMaxHours)
            {
                throw new DomainException(
                    $"Hours for {allocation.Project.Name} cannot exceed {projectMaxHours} (based on {allocation.UtilisationPercent}% allocation).");
            }

            if (entry.Hours > 0)
            {
                ActivityTagCatalog.ValidateTags(entry.ActivityTags);
            }

            totalHours += entry.Hours;
        }

        if (totalHours > maxWeeklyHours)
        {
            throw new DomainException($"Total hours cannot exceed the configured maximum of {maxWeeklyHours} per week.");
        }
    }

    public static decimal CalculateProjectMaxHours(int utilisationPercent, int maxWeeklyHours) =>
        Math.Round(maxWeeklyHours * utilisationPercent / 100m, 2, MidpointRounding.AwayFromZero);

    private static void ValidateWeekStart(DateOnly weekStart, ILogger? logger)
    {
        ActiveDateHelper.EnsureMondayWeekStart(weekStart);

        if (weekStart > ActiveDateHelper.GetCurrentWeekStartUtc())
        {
            logger?.LogWarning(
                "Timesheet validation rejected. WeekStart={WeekStart}, Reason=FutureWeek",
                weekStart);

            throw new DomainException("You cannot submit a timesheet for a future week.");
        }
    }

    private static void ValidateUniqueProjectIds(IReadOnlyList<SubmitTimesheetEntryRequest> entries)
    {
        var duplicateProjectId = entries
            .GroupBy(entry => entry.ProjectId)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;

        if (duplicateProjectId.HasValue)
        {
            throw new DomainException($"Duplicate timesheet entry for project ID {duplicateProjectId.Value}.");
        }
    }
}
