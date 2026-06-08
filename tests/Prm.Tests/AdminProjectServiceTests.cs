using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Prm.Application.DTOs.Admin;
using Prm.Application.Interfaces;
using Prm.Application.Services.Admin;
using Prm.Domain.Entities;
using Prm.Domain.Enums;
using Prm.Domain.Exceptions;

namespace Prm.Tests;

public class AdminProjectServiceTests
{
    private readonly Mock<IProjectRepository> _projects = new();
    private readonly Mock<IUserRepository> _users = new();

    [Fact]
    public async Task CreateAsync_Throws_When_Manager_Inactive()
    {
        _users.Setup(u => u.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(new User
        {
            Id = 2,
            Role = UserRole.Manager,
            IsActive = false
        });

        var service = CreateService();
        var request = new CreateProjectRequest(
            "Valid Project",
            "Description",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            "Planned",
            2,
            100);

        await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task AddMilestoneAsync_Throws_When_StoryPoints_Exceed_Budget()
    {
        var project = new Project
        {
            Id = 1,
            Name = "Gamma",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31),
            TotalStoryPoints = 60,
            Manager = new User { FullName = "Manager" }
        };

        _projects.Setup(p => p.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        _projects.Setup(p => p.GetMilestoneStoryPointsSumAsync(1, null, It.IsAny<CancellationToken>())).ReturnsAsync(50);

        var service = CreateService();
        var request = new AddMilestoneRequest("Overflow", new DateOnly(2026, 6, 1), 20);

        await Assert.ThrowsAsync<DomainException>(() => service.AddMilestoneAsync(1, request));
    }

    private AdminProjectService CreateService() =>
        new(_projects.Object, _users.Object, NullLogger<AdminProjectService>.Instance);
}
