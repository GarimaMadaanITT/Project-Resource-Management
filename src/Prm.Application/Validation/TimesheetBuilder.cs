using Prm.Application.Common;
using Prm.Application.DTOs.Employee;
using Prm.Domain.Entities;
using Prm.Domain.Enums;

namespace Prm.Application.Validation;

public static class TimesheetBuilder
{
    public static Timesheet Build(
        int employeeId,
        DateOnly weekStart,
        IReadOnlyList<SubmitTimesheetEntryRequest> entries)
    {
        var timesheet = new Timesheet
        {
            EmployeeId = employeeId,
            WeekStart = weekStart,
            Status = TimesheetStatus.Submitted,
            TotalHours = entries.Sum(entry => entry.Hours)
        };

        foreach (var entry in entries)
        {
            timesheet.Entries.Add(new TimesheetEntry
            {
                ProjectId = entry.ProjectId,
                Hours = entry.Hours,
                ActivityTags = entry.Hours > 0
                    ? ActivityTagCatalog.SerializeTagsForStorage(entry.ActivityTags)
                    : string.Empty
            });
        }

        return timesheet;
    }
}
