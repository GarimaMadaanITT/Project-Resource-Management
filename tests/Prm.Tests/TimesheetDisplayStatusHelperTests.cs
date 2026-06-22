using Prm.Application.Services.Notifications;
using Prm.Domain.Enums;

namespace Prm.Tests;

public class TimesheetDisplayStatusHelperTests
{
    private static readonly DateOnly WeekStart = new(2026, 6, 1);

    [Fact]
    public void Resolve_Returns_Submitted_When_Timesheet_Exists()
    {
        var status = TimesheetDisplayStatusHelper.Resolve(
            hasSubmittedTimesheet: true,
            complianceStatus: null,
            WeekStart,
            new DateOnly(2026, 6, 10));

        Assert.Equal(TimesheetStatus.Submitted.ToString(), status);
    }

    [Fact]
    public void Resolve_Returns_Pending_During_Grace()
    {
        var status = TimesheetDisplayStatusHelper.Resolve(
            hasSubmittedTimesheet: false,
            complianceStatus: TimesheetComplianceStatus.Pending,
            WeekStart,
            new DateOnly(2026, 6, 9));

        Assert.Equal(TimesheetStatus.Pending.ToString(), status);
    }

    [Fact]
    public void Resolve_Returns_Missed_After_Grace_Without_Compliance_Record()
    {
        var status = TimesheetDisplayStatusHelper.Resolve(
            hasSubmittedTimesheet: false,
            complianceStatus: null,
            WeekStart,
            new DateOnly(2026, 6, 11));

        Assert.Equal(TimesheetStatus.Missed.ToString(), status);
    }
}
