using Moq;
using Prm.Application.Interfaces;
using Prm.Application.Services.Shared;
using Prm.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace Prm.Tests;

public class AuditLogServiceTests
{
    private readonly Mock<IAuditLogRepository> _repository = new();

    [Fact]
    public async Task AuditAsync_Skips_When_Old_And_New_Are_Equal()
    {
        var service = CreateService();
        var snapshot = new { id = 1, name = "Alpha" };

        await service.AuditAsync(
            "Project",
            1,
            "Updated",
            snapshot,
            snapshot,
            99,
            "Admin",
            "User");

        _repository.Verify(
            repository => repository.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AuditAsync_Persists_When_Values_Differ()
    {
        var service = CreateService();

        await service.AuditAsync(
            "User",
            5,
            "Created",
            null,
            new { id = 5, username = "new.user" },
            1,
            "Admin",
            "User");

        _repository.Verify(
            repository => repository.AddAsync(
                It.Is<AuditLog>(log =>
                    log.EntityName == "User"
                    && log.EntityId == 5
                    && log.Action == "Created"
                    && log.OldValue == null
                    && log.NewValue != null
                    && log.PerformedByUserId == 1
                    && log.PerformedByRole == "Admin"
                    && log.Source == "User"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private AuditLogService CreateService() =>
        new(_repository.Object, NullLogger<AuditLogService>.Instance);
}
