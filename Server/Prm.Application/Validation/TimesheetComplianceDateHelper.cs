namespace Prm.Application.Validation;

using Prm.Domain.Enums;

public enum TimesheetComplianceAction
{
    None,
    Reminder1,
    Reminder2,
    Freeze
}

public static class TimesheetComplianceDateHelper
{
    public static DateOnly GetFridayDeadline(DateOnly weekStart) => weekStart.AddDays(4);

    public static bool IsPastDeadline(DateOnly today, DateOnly weekStart) =>
        today > GetFridayDeadline(weekStart);

    public static bool IsWorkingDay(DateOnly date) =>
        date.DayOfWeek is >= DayOfWeek.Monday and <= DayOfWeek.Friday;

    public static TimesheetComplianceAction GetDueAction(
        DateOnly today,
        int reminderCount,
        TimesheetComplianceStatus? complianceStatus)
    {
        if (complianceStatus == TimesheetComplianceStatus.Missed)
        {
            return TimesheetComplianceAction.None;
        }

        if (reminderCount <= 0 && today.DayOfWeek >= DayOfWeek.Monday)
        {
            return TimesheetComplianceAction.Reminder1;
        }

        if (reminderCount == 1 && today.DayOfWeek >= DayOfWeek.Tuesday)
        {
            return TimesheetComplianceAction.Reminder2;
        }

        if (reminderCount >= 2 && today.DayOfWeek >= DayOfWeek.Wednesday)
        {
            return TimesheetComplianceAction.Freeze;
        }

        return TimesheetComplianceAction.None;
    }
}
