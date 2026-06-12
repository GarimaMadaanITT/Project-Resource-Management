using Prm.Application.Common;
using Prm.Application.DTOs.Employee;
using Prm.Application.Validation;
using Prm.Domain.Entities;
using Prm.Domain.Enums;
using Prm.Domain.Exceptions;

namespace Prm.Tests;

public class TimesheetValidatorTests
{
    [Fact]
    public void ValidateSubmitRequest_Allows_Valid_Entry()
    {
        var request = CreateRequest(1, 20, ["Bug Fixing"]);
        var allocations = CreateWeekAllocations(1, "Beta CRM", 50);

        TimesheetValidator.ValidateSubmitRequest(
            request,
            new DateOnly(2026, 5, 4),
            allocations,
            maxWeeklyHours: 40,
            timesheetAlreadyExists: false);
    }

    [Fact]
    public void ValidateSubmitRequest_Throws_Conflict_When_Week_Already_Submitted()
    {
        var request = CreateRequest(1, 20, ["Bug Fixing"]);
        var allocations = CreateWeekAllocations(1, "Beta CRM", 50);

        Assert.Throws<ConflictException>(() =>
            TimesheetValidator.ValidateSubmitRequest(
                request,
                new DateOnly(2026, 5, 4),
                allocations,
                maxWeeklyHours: 40,
                timesheetAlreadyExists: true));
    }

    [Fact]
    public void ValidateSubmitRequest_Throws_When_Project_Not_Allocated()
    {
        var request = CreateRequest(99, 10, ["Bug Fixing"]);
        var allocations = CreateWeekAllocations(1, "Beta CRM", 50);

        Assert.Throws<DomainException>(() =>
            TimesheetValidator.ValidateSubmitRequest(
                request,
                new DateOnly(2026, 5, 4),
                allocations,
                maxWeeklyHours: 40,
                timesheetAlreadyExists: false));
    }

    [Fact]
    public void ValidateSubmitRequest_Throws_When_Hours_Exceed_Project_Cap()
    {
        var request = CreateRequest(1, 25, ["Bug Fixing"]);
        var allocations = CreateWeekAllocations(1, "Beta CRM", 50);

        Assert.Throws<DomainException>(() =>
            TimesheetValidator.ValidateSubmitRequest(
                request,
                new DateOnly(2026, 5, 4),
                allocations,
                maxWeeklyHours: 40,
                timesheetAlreadyExists: false));
    }

    [Fact]
    public void ValidateSubmitRequest_Throws_When_No_Allocations_For_Week()
    {
        var request = CreateRequest(1, 10, ["Bug Fixing"]);

        Assert.Throws<DomainException>(() =>
            TimesheetValidator.ValidateSubmitRequest(
                request,
                new DateOnly(2026, 5, 4),
                Array.Empty<Allocation>(),
                maxWeeklyHours: 40,
                timesheetAlreadyExists: false));
    }

    [Fact]
    public void ValidateSubmitRequest_Throws_When_WeekStart_Is_Not_Monday()
    {
        var request = CreateRequest(1, 10, ["Bug Fixing"]);
        var allocations = CreateWeekAllocations(1, "Beta CRM", 50);

        Assert.Throws<DomainException>(() =>
            TimesheetValidator.ValidateSubmitRequest(
                request,
                new DateOnly(2026, 5, 6),
                allocations,
                maxWeeklyHours: 40,
                timesheetAlreadyExists: false));
    }

    [Fact]
    public void CalculateProjectMaxHours_Returns_Expected_Value()
    {
        Assert.Equal(20m, TimesheetValidator.CalculateProjectMaxHours(50, 40));
    }

    private static SubmitTimesheetRequest CreateRequest(
        int projectId,
        decimal hours,
        IReadOnlyList<string> tags) =>
        new(
            new DateOnly(2026, 5, 4),
            [new SubmitTimesheetEntryRequest(projectId, hours, tags)]);

    private static List<Allocation> CreateWeekAllocations(int projectId, string projectName, int utilisation) =>
    [
        new()
        {
            ProjectId = projectId,
            UtilisationPercent = utilisation,
            FromDate = new DateOnly(2026, 3, 1),
            ToDate = new DateOnly(2026, 8, 31),
            Project = new Project { Id = projectId, Name = projectName, Status = ProjectStatus.Active }
        }
    ];
}
