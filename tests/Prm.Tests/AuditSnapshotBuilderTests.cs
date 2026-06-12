using System.Text.Json;
using Prm.Application.Common;
using Prm.Domain.Entities;
using Prm.Domain.Enums;

namespace Prm.Tests;

public class AuditSnapshotBuilderTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void UserSnapshot_Includes_Expected_Fields()
    {
        var user = TestDataHelpers.CreateUser(UserRole.Admin);
        user.Id = 3;
        user.Username = "admin";
        user.IsTemporaryPassword = true;

        var json = JsonSerializer.Serialize(AuditSnapshotBuilder.UserSnapshot(user), JsonOptions);

        Assert.Contains("\"id\":3", json);
        Assert.Contains("\"username\":\"admin\"", json);
        Assert.Contains("\"role\":\"Admin\"", json);
        Assert.Contains("\"isActive\":true", json);
        Assert.Contains("\"isTemporaryPassword\":true", json);
    }

    [Fact]
    public void ResourceProfileSnapshot_Includes_Status_And_Manager()
    {
        var user = TestDataHelpers.CreateUser(UserRole.Employee);
        user.Department = Department.Engineering;

        var resourceProfile = new ResourceProfile
        {
            Id = 7,
            ManagerUserId = 2,
            ResourceStatus = ResourceStatus.Bench,
            User = user
        };

        var json = JsonSerializer.Serialize(AuditSnapshotBuilder.ResourceProfileSnapshot(resourceProfile), JsonOptions);

        Assert.Contains("\"id\":7", json);
        Assert.Contains("\"department\":\"Engineering\"", json);
        Assert.Contains("\"status\":\"Bench\"", json);
        Assert.Contains("\"managerUserId\":2", json);
    }

    [Fact]
    public void MissedTimesheetSnapshot_Includes_Week_And_ResourceProfile()
    {
        var weekStart = new DateOnly(2026, 5, 4);
        var json = JsonSerializer.Serialize(AuditSnapshotBuilder.MissedTimesheetSnapshot(weekStart, 12), JsonOptions);

        Assert.Contains("\"resourceProfileId\":12", json);
        Assert.Contains("\"weekStart\":\"2026-05-04\"", json);
    }
}
