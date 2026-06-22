using Prm.Application.Validation;
using Prm.Domain.Enums;

namespace Prm.Application.Services.Notifications;

public static class TimesheetDisplayStatusHelper
{
    public static string Resolve(
        bool hasSubmittedTimesheet,
        TimesheetComplianceStatus? complianceStatus,
        DateOnly weekStart,
        DateOnly today)
    {
        if (hasSubmittedTimesheet)
        {
            return TimesheetStatus.Submitted.ToString();
        }

        if (complianceStatus == TimesheetComplianceStatus.Missed)
        {
            return TimesheetStatus.Missed.ToString();
        }

        if (complianceStatus == TimesheetComplianceStatus.Pending)
        {
            return TimesheetStatus.Pending.ToString();
        }

        if (TimesheetComplianceDateHelper.IsPastDeadline(today, weekStart))
        {
            return today.DayOfWeek >= DayOfWeek.Wednesday
                ? TimesheetStatus.Missed.ToString()
                : TimesheetStatus.Pending.ToString();
        }

        return TimesheetStatus.Pending.ToString();
    }
}
