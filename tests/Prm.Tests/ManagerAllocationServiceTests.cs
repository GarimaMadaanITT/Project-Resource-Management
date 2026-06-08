using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Prm.Application.DTOs.Manager;
using Prm.Application.Interfaces;
using Prm.Application.Services.Manager;
using Prm.Domain.Entities;
using Prm.Domain.Enums;
using Prm.Domain.Exceptions;

namespace Prm.Tests;

public class ManagerAllocationServiceTests
{
    private readonly Mock<IManagerContextService> _context = new();
    private readonly Mock<IEmployeeRepository> _employees = new();
    private readonly Mock<IProjectRepository> _projects = new();
    private readonly Mock<IAllocationRepository> _allocations = new();

    [Fact]
    public async Task CreateAsync_Does_Not_Save_When_Validation_Fails()
    {
        SetupManagerContext();
        _projects.Setup(p => p.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Project
            {
                Id = 1,
                Name = "Alpha Portal",
                Status = ProjectStatus.Active,
                ManagerUserId = 2
            });

        _employees.Setup(e => e.GetTeamMemberAsync(10, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Employee
            {
                Id = 5,
                ManagerId = 10,
                IsActive = true,
                User = new User { FullName = "Anil Mehta" }
            });

        _allocations.Setup(a => a.GetByEmployeeIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Allocation>
            {
                new()
                {
                    EmployeeId = 5,
                    UtilisationPercent = 60,
                    FromDate = new DateOnly(2026, 3, 1),
                    ToDate = new DateOnly(2026, 6, 30)
                }
            });

        var service = CreateService();
        var request = new CreateManagerAllocationRequest(
            1,
            5,
            50,
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 7, 31));

        await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(2, request));
        _allocations.Verify(a => a.AddAsync(It.IsAny<Allocation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private void SetupManagerContext()
    {
        _context.Setup(c => c.ResolveAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ManagerContext(2, 10));
    }

    private ManagerAllocationService CreateService() =>
        new(
            _context.Object,
            _employees.Object,
            _projects.Object,
            _allocations.Object,
            NullLogger<ManagerAllocationService>.Instance);
}
