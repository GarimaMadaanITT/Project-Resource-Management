using Microsoft.Extensions.Logging;
using Prm.Application.Common;
using Prm.Application.DTOs.Admin;
using Prm.Application.DTOs.Notifications;
using Prm.Application.Interfaces;
using Prm.Application.Services.Notifications;
using Prm.Application.Validation;
using Prm.Domain.Entities;
using Prm.Domain.Enums;

namespace Prm.Application.Services.Scheduler;

public class TimesheetComplianceNotificationService : ITimesheetMissedDetectionService
{
    private readonly IResourceProfileRepository _resourceProfiles;
    private readonly ITimesheetRepository _timesheets;
    private readonly ITimesheetComplianceRepository _complianceRepository;
    private readonly IAuditLogService _auditLog;
    private readonly NotificationDispatchService _notificationDispatch;
    private readonly ILogger<TimesheetComplianceNotificationService> _logger;

    public TimesheetComplianceNotificationService(
        IResourceProfileRepository resourceProfiles,
        ITimesheetRepository timesheets,
        ITimesheetComplianceRepository complianceRepository,
        IAuditLogService auditLog,
        NotificationDispatchService notificationDispatch,
        ILogger<TimesheetComplianceNotificationService> logger)
    {
        _resourceProfiles = resourceProfiles;
        _timesheets = timesheets;
        _complianceRepository = complianceRepository;
        _auditLog = auditLog;
        _notificationDispatch = notificationDispatch;
        _logger = logger;
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var today = ActiveDateHelper.TodayUtc;
        var targetWeek = ActiveDateHelper.GetPreviousCompletedWeekStart();

        if (!TimesheetComplianceDateHelper.IsPastDeadline(today, targetWeek))
        {
            _logger.LogInformation(
                "Timesheet compliance skipped. WeekStart={WeekStart}, Reason=BeforeDeadline",
                targetWeek);
            return 0;
        }

        var resourceProfiles = await _resourceProfiles.GetAllActiveWithAllocationsAsync(cancellationToken);
        var processedCount = 0;

        foreach (var resourceProfile in resourceProfiles)
        {
            var hadAllocation = resourceProfile.Allocations
                .Any(allocation => ActiveDateHelper.IsAllocationActiveDuringWeek(allocation, targetWeek));

            if (!hadAllocation)
            {
                continue;
            }

            if (resourceProfile.TimesheetSubmissionFrozen)
            {
                continue;
            }

            var timesheetExists = await _timesheets.ExistsForResourceProfileWeekAsync(
                resourceProfile.Id,
                targetWeek,
                cancellationToken);

            if (timesheetExists)
            {
                var existingCompliance = await _complianceRepository.GetByResourceProfileAndWeekAsync(
                    resourceProfile.Id,
                    targetWeek,
                    cancellationToken);

                if (existingCompliance is not null)
                {
                    await _complianceRepository.DeleteAsync(existingCompliance, cancellationToken);
                }

                continue;
            }

            processedCount += await ProcessMissingTimesheetAsync(resourceProfile, targetWeek, today, cancellationToken);
        }

        _logger.LogInformation(
            "Timesheet compliance notification completed. Processed={Processed}, WeekStart={WeekStart}",
            processedCount,
            targetWeek);

        return processedCount;
    }

    public async Task<TimesheetComplianceForceResponse> ForceAdvanceComplianceAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        var resourceProfile = await FindActiveProfileByUsernameAsync(username, cancellationToken);
        if (resourceProfile is null)
        {
            return new TimesheetComplianceForceResponse(
                username,
                false,
                false,
                false,
                false,
                $"Active employee '{username}' not found.");
        }

        var targetWeek = ActiveDateHelper.GetPreviousCompletedWeekStart();
        var timesheetExists = await _timesheets.ExistsForResourceProfileWeekAsync(
            resourceProfile.Id,
            targetWeek,
            cancellationToken);

        if (timesheetExists)
        {
            return new TimesheetComplianceForceResponse(
                username,
                false,
                false,
                false,
                resourceProfile.TimesheetSubmissionFrozen,
                $"Timesheet already submitted for week {targetWeek:dd-MMM-yyyy}.");
        }

        var compliance = await _complianceRepository.GetByResourceProfileAndWeekAsync(
            resourceProfile.Id,
            targetWeek,
            cancellationToken);

        var reminder1Sent = false;
        var reminder2Sent = false;
        var frozen = false;

        if (compliance is null || compliance.ReminderCount < 1)
        {
            reminder1Sent = await SendReminder1Async(resourceProfile, targetWeek, compliance, cancellationToken) > 0;
            compliance = await _complianceRepository.GetByResourceProfileAndWeekAsync(
                resourceProfile.Id,
                targetWeek,
                cancellationToken);
        }

        if (compliance is not null && compliance.ReminderCount < 2)
        {
            resourceProfile = await RequireActiveProfileByUsernameAsync(username, cancellationToken)
                ?? resourceProfile;
            reminder2Sent = await SendReminder2Async(resourceProfile, targetWeek, compliance, cancellationToken) > 0;
            compliance = await _complianceRepository.GetByResourceProfileAndWeekAsync(
                resourceProfile.Id,
                targetWeek,
                cancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(1.2), cancellationToken);
        }

        if (compliance is not null && !compliance.FreezeNotifiedAt.HasValue)
        {
            resourceProfile = await RequireActiveProfileByUsernameAsync(username, cancellationToken)
                ?? resourceProfile;
            frozen = await FreezeAsync(resourceProfile, targetWeek, compliance, cancellationToken) > 0;
        }

        resourceProfile = await RequireActiveProfileByUsernameAsync(username, cancellationToken)
            ?? resourceProfile;

        var message =
            $"Week {targetWeek:dd-MMM-yyyy}: Reminder1={reminder1Sent || (compliance?.ReminderCount >= 1)}, " +
            $"Reminder2={reminder2Sent || (compliance?.ReminderCount >= 2)}, " +
            $"Frozen={resourceProfile.TimesheetSubmissionFrozen}.";

        _logger.LogInformation(
            "Forced timesheet compliance for {Username}. {Message}",
            username,
            message);

        return new TimesheetComplianceForceResponse(
            username,
            reminder1Sent || (compliance?.ReminderCount >= 1),
            reminder2Sent || (compliance?.ReminderCount >= 2),
            frozen || resourceProfile.TimesheetSubmissionFrozen,
            resourceProfile.TimesheetSubmissionFrozen,
            message);
    }

    private async Task<ResourceProfile?> FindActiveProfileByUsernameAsync(
        string username,
        CancellationToken cancellationToken)
    {
        var profiles = await _resourceProfiles.GetAllActiveWithAllocationsAsync(cancellationToken);
        return profiles.FirstOrDefault(profile =>
            profile.User.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<ResourceProfile?> RequireActiveProfileByUsernameAsync(
        string username,
        CancellationToken cancellationToken) =>
        await FindActiveProfileByUsernameAsync(username, cancellationToken);

    private async Task<int> ProcessMissingTimesheetAsync(
        ResourceProfile resourceProfile,
        DateOnly targetWeek,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        if (resourceProfile.TimesheetSubmissionFrozen)
        {
            return 0;
        }

        var compliance = await _complianceRepository.GetByResourceProfileAndWeekAsync(
            resourceProfile.Id,
            targetWeek,
            cancellationToken);

        var reminderCount = compliance?.ReminderCount ?? 0;
        var complianceStatus = compliance?.Status;
        var dueAction = TimesheetComplianceDateHelper.GetDueAction(today, reminderCount, complianceStatus);

        if (dueAction == TimesheetComplianceAction.None)
        {
            return 0;
        }

        return dueAction switch
        {
            TimesheetComplianceAction.Reminder1 => await SendReminder1Async(resourceProfile, targetWeek, compliance, cancellationToken),
            TimesheetComplianceAction.Reminder2 when compliance is null =>
                await SendReminder1Async(resourceProfile, targetWeek, null, cancellationToken),
            TimesheetComplianceAction.Reminder2 => await SendReminder2Async(resourceProfile, targetWeek, compliance, cancellationToken),
            TimesheetComplianceAction.Freeze when compliance is null => 0,
            TimesheetComplianceAction.Freeze => await FreezeAsync(resourceProfile, targetWeek, compliance, cancellationToken),
            _ => 0
        };
    }

    private async Task<int> SendReminder1Async(
        ResourceProfile resourceProfile,
        DateOnly targetWeek,
        TimesheetCompliance? compliance,
        CancellationToken cancellationToken)
    {
        compliance ??= new TimesheetCompliance
        {
            ResourceProfileId = resourceProfile.Id,
            WeekStart = targetWeek,
            Status = TimesheetComplianceStatus.Pending,
            ReminderCount = 0
        };

        var referenceKey = $"compliance:{resourceProfile.Id}:{targetWeek:yyyy-MM-dd}:reminder1";
        var subject = EmailTemplateBuilder.Reminder1Subject(targetWeek);
        var body = EmailTemplateBuilder.BuildTimesheetReminder1(resourceProfile.User.FullName, targetWeek);

        var sent = await _notificationDispatch.SendIfNotSentAsync(
            NotificationType.TimesheetReminder1,
            referenceKey,
            new EmailMessage(resourceProfile.User.Email, subject, body),
            cancellationToken);

        if (!sent)
        {
            return 0;
        }

        compliance.Status = TimesheetComplianceStatus.Pending;
        compliance.ReminderCount = 1;
        compliance.Reminder1SentAt = DateTime.UtcNow;

        if (compliance.Id == 0)
        {
            await _complianceRepository.AddAsync(compliance, cancellationToken);

            await _auditLog.AuditAsync(
                AuditConstants.EntityNames.Timesheet,
                resourceProfile.Id,
                AuditConstants.Actions.MissedTimesheetDetected,
                null,
                AuditSnapshotBuilder.MissedTimesheetSnapshot(targetWeek, resourceProfile.Id),
                null,
                null,
                AuditConstants.Sources.Scheduler,
                cancellationToken);
        }
        else
        {
            await _complianceRepository.UpdateAsync(compliance, cancellationToken);
        }

        return 1;
    }

    private async Task<int> SendReminder2Async(
        ResourceProfile resourceProfile,
        DateOnly targetWeek,
        TimesheetCompliance compliance,
        CancellationToken cancellationToken)
    {
        var referenceKey = $"compliance:{resourceProfile.Id}:{targetWeek:yyyy-MM-dd}:reminder2";
        var subject = EmailTemplateBuilder.Reminder2Subject(targetWeek);
        var body = EmailTemplateBuilder.BuildTimesheetReminder2(resourceProfile.User.FullName, targetWeek);

        var sent = await _notificationDispatch.SendIfNotSentAsync(
            NotificationType.TimesheetReminder2,
            referenceKey,
            new EmailMessage(resourceProfile.User.Email, subject, body),
            cancellationToken);

        if (!sent)
        {
            return 0;
        }

        compliance.ReminderCount = 2;
        compliance.Reminder2SentAt = DateTime.UtcNow;
        await _complianceRepository.UpdateAsync(compliance, cancellationToken);

        return 1;
    }

    private async Task<int> FreezeAsync(
        ResourceProfile resourceProfile,
        DateOnly targetWeek,
        TimesheetCompliance compliance,
        CancellationToken cancellationToken)
    {
        if (compliance.FreezeNotifiedAt.HasValue)
        {
            return 0;
        }

        resourceProfile.TimesheetSubmissionFrozen = true;
        resourceProfile.TimesheetFrozenAt = DateTime.UtcNow;
        await _resourceProfiles.UpdateAsync(resourceProfile, cancellationToken);

        compliance.Status = TimesheetComplianceStatus.Missed;
        compliance.FreezeNotifiedAt = DateTime.UtcNow;
        await _complianceRepository.UpdateAsync(compliance, cancellationToken);

        await _auditLog.AuditAsync(
            AuditConstants.EntityNames.ResourceProfile,
            resourceProfile.Id,
            AuditConstants.Actions.TimesheetAccessFrozen,
            null,
            AuditSnapshotBuilder.TimesheetFreezeSnapshot(resourceProfile.Id, targetWeek),
            null,
            null,
            AuditConstants.Sources.Scheduler,
            cancellationToken);

        var employeeReferenceKey = $"compliance:{resourceProfile.Id}:{targetWeek:yyyy-MM-dd}:freeze:employee";
        await SendFreezeEmailSafelyAsync(
            NotificationType.TimesheetFrozenEmployee,
            employeeReferenceKey,
            new EmailMessage(
                resourceProfile.User.Email,
                EmailTemplateBuilder.FrozenEmployeeSubject(),
                EmailTemplateBuilder.BuildTimesheetFrozenEmployee(resourceProfile.User.FullName, targetWeek)),
            cancellationToken);

        if (resourceProfile.ManagerUserId.HasValue && resourceProfile.Manager is not null)
        {
            await Task.Delay(TimeSpan.FromSeconds(1.2), cancellationToken);

            var managerReferenceKey = $"compliance:{resourceProfile.Id}:{targetWeek:yyyy-MM-dd}:freeze:manager";
            await SendFreezeEmailSafelyAsync(
                NotificationType.TimesheetFrozenManager,
                managerReferenceKey,
                new EmailMessage(
                    resourceProfile.Manager.Email,
                    EmailTemplateBuilder.FrozenManagerSubject(resourceProfile.User.FullName),
                    EmailTemplateBuilder.BuildTimesheetFrozenManager(
                        resourceProfile.Manager.FullName,
                        resourceProfile.User.FullName,
                        targetWeek)),
                cancellationToken);
        }

        return 1;
    }

    private async Task SendFreezeEmailSafelyAsync(
        NotificationType notificationType,
        string referenceKey,
        EmailMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            await _notificationDispatch.SendIfNotSentAsync(
                notificationType,
                referenceKey,
                message,
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Freeze email failed but access remains frozen. To={To}, Subject={Subject}",
                message.To,
                message.Subject);
        }
    }
}
