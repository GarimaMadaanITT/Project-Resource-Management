using Prm.Application.Validation;
using Prm.Domain.Entities;
using Prm.Domain.Enums;
using Prm.Domain.Exceptions;

namespace Prm.Tests;

public class AllocationValidatorTests
{
    [Fact]
    public void ValidateCreateRequest_Allows_NonOverlapping_Periods()
    {
        var project = CreateProject(ProjectStatus.Active);
        var resourceProfile = CreateResourceProfile();
        var existing = new List<Allocation>
        {
            new()
            {
                ResourceProfileId = 1,
                UtilisationPercent = 50,
                FromDate = Future(10),
                ToDate = Future(100)
            }
        };

        AllocationValidator.ValidateCreateRequest(
            project,
            resourceProfile,
            50,
            Future(101),
            Future(190),
            existing);
    }

    [Fact]
    public void ValidateCreateRequest_Allows_50_Plus_50_Overlap()
    {
        var project = CreateProject(ProjectStatus.Active);
        var resourceProfile = CreateResourceProfile();
        var existing = new List<Allocation>
        {
            new()
            {
                ResourceProfileId = 1,
                UtilisationPercent = 50,
                FromDate = Future(10),
                ToDate = Future(120)
            }
        };

        AllocationValidator.ValidateCreateRequest(
            project,
            resourceProfile,
            50,
            Future(40),
            Future(150),
            existing);
    }

    [Fact]
    public void ValidateCreateRequest_Throws_When_Overlap_Exceeds_100()
    {
        var project = CreateProject(ProjectStatus.Active);
        var resourceProfile = CreateResourceProfile();
        var existing = new List<Allocation>
        {
            new()
            {
                ResourceProfileId = 1,
                UtilisationPercent = 60,
                FromDate = Future(10),
                ToDate = Future(120)
            }
        };

        Assert.Throws<DomainException>(() =>
            AllocationValidator.ValidateCreateRequest(
                project,
                resourceProfile,
                50,
                Future(40),
                Future(150),
                existing));
    }

    [Fact]
    public void ValidateCreateRequest_Throws_When_Project_Not_Active_Or_Planned()
    {
        var project = CreateProject(ProjectStatus.Completed);
        var resourceProfile = CreateResourceProfile();

        Assert.Throws<DomainException>(() =>
            AllocationValidator.ValidateCreateRequest(
                project,
                resourceProfile,
                50,
                Future(10),
                Future(120),
                Array.Empty<Allocation>()));
    }

    [Fact]
    public void ValidateCreateRequest_Throws_When_Employee_Inactive()
    {
        var project = CreateProject(ProjectStatus.Active);
        var resourceProfile = CreateResourceProfile(isActive: false);

        Assert.Throws<DomainException>(() =>
            AllocationValidator.ValidateCreateRequest(
                project,
                resourceProfile,
                50,
                Future(10),
                Future(120),
                Array.Empty<Allocation>()));
    }

    [Fact]
    public void ValidateCreateRequest_Throws_When_From_Date_In_Past()
    {
        var project = CreateProject(ProjectStatus.Active);
        var resourceProfile = CreateResourceProfile();

        Assert.Throws<DomainException>(() =>
            AllocationValidator.ValidateCreateRequest(
                project,
                resourceProfile,
                50,
                ActiveDateHelper.TodayUtc.AddDays(-1),
                Future(30),
                Array.Empty<Allocation>()));
    }

    private static DateOnly Future(int daysFromToday) => ActiveDateHelper.TodayUtc.AddDays(daysFromToday);

    private static Project CreateProject(ProjectStatus status) =>
        new()
        {
            Id = 1,
            Name = "Alpha Portal",
            Status = status,
            ManagerUserId = 2
        };

    private static ResourceProfile CreateResourceProfile(bool isActive = true) =>
        TestDataHelpers.CreateResourceProfile(1, isActive, fullName: "Anil Mehta");
}
