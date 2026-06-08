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
        var employee = CreateEmployee();
        var existing = new List<Allocation>
        {
            new()
            {
                EmployeeId = 1,
                UtilisationPercent = 50,
                FromDate = new DateOnly(2026, 1, 1),
                ToDate = new DateOnly(2026, 3, 31)
            }
        };

        AllocationValidator.ValidateCreateRequest(
            project,
            employee,
            50,
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 6, 30),
            existing);
    }

    [Fact]
    public void ValidateCreateRequest_Allows_50_Plus_50_Overlap()
    {
        var project = CreateProject(ProjectStatus.Active);
        var employee = CreateEmployee();
        var existing = new List<Allocation>
        {
            new()
            {
                EmployeeId = 1,
                UtilisationPercent = 50,
                FromDate = new DateOnly(2026, 3, 1),
                ToDate = new DateOnly(2026, 6, 30)
            }
        };

        AllocationValidator.ValidateCreateRequest(
            project,
            employee,
            50,
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 7, 31),
            existing);
    }

    [Fact]
    public void ValidateCreateRequest_Throws_When_Overlap_Exceeds_100()
    {
        var project = CreateProject(ProjectStatus.Active);
        var employee = CreateEmployee();
        var existing = new List<Allocation>
        {
            new()
            {
                EmployeeId = 1,
                UtilisationPercent = 60,
                FromDate = new DateOnly(2026, 3, 1),
                ToDate = new DateOnly(2026, 6, 30)
            }
        };

        Assert.Throws<DomainException>(() =>
            AllocationValidator.ValidateCreateRequest(
                project,
                employee,
                50,
                new DateOnly(2026, 4, 1),
                new DateOnly(2026, 7, 31),
                existing));
    }

    [Fact]
    public void ValidateCreateRequest_Throws_When_Project_Not_Active_Or_Planned()
    {
        var project = CreateProject(ProjectStatus.Completed);
        var employee = CreateEmployee();

        Assert.Throws<DomainException>(() =>
            AllocationValidator.ValidateCreateRequest(
                project,
                employee,
                50,
                new DateOnly(2026, 4, 1),
                new DateOnly(2026, 7, 31),
                Array.Empty<Allocation>()));
    }

    [Fact]
    public void ValidateCreateRequest_Throws_When_Employee_Inactive()
    {
        var project = CreateProject(ProjectStatus.Active);
        var employee = CreateEmployee(isActive: false);

        Assert.Throws<DomainException>(() =>
            AllocationValidator.ValidateCreateRequest(
                project,
                employee,
                50,
                new DateOnly(2026, 4, 1),
                new DateOnly(2026, 7, 31),
                Array.Empty<Allocation>()));
    }

    private static Project CreateProject(ProjectStatus status) =>
        new()
        {
            Id = 1,
            Name = "Alpha Portal",
            Status = status,
            ManagerUserId = 2
        };

    private static Employee CreateEmployee(bool isActive = true) =>
        new()
        {
            Id = 1,
            IsActive = isActive,
            User = new User { FullName = "Anil Mehta" }
        };
}
