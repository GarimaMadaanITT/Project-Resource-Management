using Prm.Application.Validation;
using Prm.Domain.Enums;

namespace Prm.Tests;

public class TimesheetComplianceDateHelperTests
{
    private static readonly DateOnly WeekStart = new(2026, 6, 1);

    [Fact]
    public void GetFridayDeadline_Returns_WeekStart_Plus_Four_Days()
    {
        Assert.Equal(new DateOnly(2026, 6, 5), TimesheetComplianceDateHelper.GetFridayDeadline(WeekStart));
    }

    [Fact]
    public void IsPastDeadline_Is_False_On_Friday()
    {
        Assert.False(TimesheetComplianceDateHelper.IsPastDeadline(new DateOnly(2026, 6, 5), WeekStart));
    }

    [Fact]
    public void IsPastDeadline_Is_True_On_Monday_After_Week()
    {
        Assert.True(TimesheetComplianceDateHelper.IsPastDeadline(new DateOnly(2026, 6, 8), WeekStart));
    }

    [Theory]
    [InlineData(DayOfWeek.Monday, 0, TimesheetComplianceAction.Reminder1)]
    [InlineData(DayOfWeek.Tuesday, 1, TimesheetComplianceAction.Reminder2)]
    [InlineData(DayOfWeek.Wednesday, 2, TimesheetComplianceAction.Freeze)]
    public void GetDueAction_Follows_Mon_Tue_Wed_Flow(
        DayOfWeek dayOfWeek,
        int reminderCount,
        TimesheetComplianceAction expected)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        while (today.DayOfWeek != dayOfWeek)
        {
            today = today.AddDays(1);
        }

        var action = TimesheetComplianceDateHelper.GetDueAction(
            today,
            reminderCount,
            TimesheetComplianceStatus.Pending);

        Assert.Equal(expected, action);
    }

    [Fact]
    public void GetDueAction_Returns_None_When_Already_Missed()
    {
        var action = TimesheetComplianceDateHelper.GetDueAction(
            new DateOnly(2026, 6, 10),
            2,
            TimesheetComplianceStatus.Missed);

        Assert.Equal(TimesheetComplianceAction.None, action);
    }
}
