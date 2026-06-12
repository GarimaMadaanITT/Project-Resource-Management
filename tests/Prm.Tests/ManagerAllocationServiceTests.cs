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
    private readonly Mock<IResourceProfileRepository> _resourceProfiles = new();
    private readonly Mock<IProjectRepository> _projects = new();
    private readonly Mock<IAllocationRepository> _allocations = new();
    private readonly Mock<IAuditLogService> _auditLog = new();

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

        _resourceProfiles.Setup(e => e.GetTeamMemberAsync(2, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceProfile
            {
                Id = 5,
                ManagerUserId = 2,
                User = new User { FullName = "Anil Mehta", IsActive = true }
            });

        _allocations.Setup(a => a.GetByResourceProfileIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Allocation>
            {
                new()
                {
                    ResourceProfileId = 5,
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
            .ReturnsAsync(new ManagerContext(2));
    }

    private ManagerAllocationService CreateService() =>
        new(
            _context.Object,
            _resourceProfiles.Object,
            _projects.Object,
            _allocations.Object,
            _auditLog.Object,
            NullLogger<ManagerAllocationService>.Instance);
}
