using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Prm.Application.Interfaces;
using Prm.Application.Services.Notifications;
using Prm.Application.Services.Scheduler;
using Prm.Application.Validation;
using Prm.Domain.Entities;
using Prm.Domain.Enums;

namespace Prm.Tests;

public class TimesheetComplianceNotificationServiceTests
{
    [Fact]
    public async Task RunAsync_Skips_Already_Frozen_Employees()
    {
        var targetWeek = ActiveDateHelper.GetPreviousCompletedWeekStart();
        var today = ActiveDateHelper.TodayUtc;
        if (!TimesheetComplianceDateHelper.IsPastDeadline(today, targetWeek))
        {
            return;
        }

        var frozenUser = TestDataHelpers.CreateUser(UserRole.Employee, fullName: "Frozen Dev");
        var frozenProfile = new ResourceProfile
        {
            Id = 1,
            UserId = frozenUser.Id,
            User = frozenUser,
            TimesheetSubmissionFrozen = true,
            Allocations =
            [
                new Allocation
                {
                    FromDate = targetWeek.AddDays(-7),
                    ToDate = targetWeek.AddDays(14),
                    UtilisationPercent = 50
                }
            ]
        };

        var resourceProfiles = new Mock<IResourceProfileRepository>();
        resourceProfiles
            .Setup(repository => repository.GetAllActiveWithAllocationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([frozenProfile]);

        var timesheets = new Mock<ITimesheetRepository>();
        timesheets
            .Setup(repository => repository.ExistsForResourceProfileWeekAsync(1, targetWeek, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var complianceRepository = new Mock<ITimesheetComplianceRepository>();
        var auditLog = new Mock<IAuditLogService>();
        var dispatch = new NotificationDispatchService(
            Mock.Of<IEmailService>(),
            Mock.Of<INotificationLogRepository>());

        var service = new TimesheetComplianceNotificationService(
            resourceProfiles.Object,
            timesheets.Object,
            complianceRepository.Object,
            auditLog.Object,
            dispatch,
            NullLogger<TimesheetComplianceNotificationService>.Instance);

        var processed = await service.RunAsync();

        Assert.Equal(0, processed);
        complianceRepository.Verify(
            repository => repository.AddAsync(It.IsAny<TimesheetCompliance>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
