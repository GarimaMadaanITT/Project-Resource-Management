using Prm.Application.Common;
using Prm.Application.DTOs.Employee;
using Prm.Application.Validation;
using Prm.Domain.Enums;
using Prm.Domain.Exceptions;

namespace Prm.Tests;

public class TimesheetBuilderTests
{
    [Fact]
    public void Build_Creates_Submitted_Timesheet_With_Total_Hours()
    {
        var timesheet = TimesheetBuilder.Build(
            resourceProfileId: 5,
            weekStart: new DateOnly(2026, 5, 11),
            entries:
            [
                new SubmitTimesheetEntryRequest(2, 18, ["Bug Fixing"])
            ]);

        Assert.Equal(5, timesheet.ResourceProfileId);
        Assert.Equal(new DateOnly(2026, 5, 11), timesheet.WeekStart);
        Assert.Equal(TimesheetStatus.Submitted, timesheet.Status);
        Assert.Equal(18, timesheet.TotalHours);
        Assert.Single(timesheet.Entries);
    }

    [Fact]
    public void Build_Serializes_Tags_Without_Other_Keyword()
    {
        var timesheet = TimesheetBuilder.Build(
            resourceProfileId: 5,
            weekStart: new DateOnly(2026, 5, 11),
            entries:
            [
                new SubmitTimesheetEntryRequest(
                    2,
                    10,
                    ["Other", "Client workshop", "Bug Fixing"])
            ]);

        var entry = timesheet.Entries.Single();
        Assert.Equal("Client workshop, Bug Fixing", entry.ActivityTags);
    }
}

public class ActivityTagCatalogTests
{
    [Fact]
    public void ValidateTags_Allows_Predefined_Tags()
    {
        ActivityTagCatalog.ValidateTags(["Microservices / Architecture", "Bug Fixing"]);
    }

    [Fact]
    public void ValidateTags_Throws_When_Custom_Tag_Without_Other()
    {
        Assert.Throws<DomainException>(() =>
            ActivityTagCatalog.ValidateTags(["Client workshop"]));
    }

    [Fact]
    public void ValidateTags_Allows_Custom_Tag_With_Other()
    {
        ActivityTagCatalog.ValidateTags(["Other", "Client workshop"]);
    }
}
