using Prm.Application.DTOs.Notifications;
using Prm.Application.Interfaces;
using Prm.Domain.Entities;
using Prm.Domain.Enums;

namespace Prm.Application.Services.Notifications;

public class NotificationDispatchService
{
    private readonly IEmailService _emailService;
    private readonly INotificationLogRepository _notificationLogs;

    public NotificationDispatchService(
        IEmailService emailService,
        INotificationLogRepository notificationLogs)
    {
        _emailService = emailService;
        _notificationLogs = notificationLogs;
    }

    public async Task<bool> SendIfNotSentAsync(
        NotificationType notificationType,
        string referenceKey,
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        if (await _notificationLogs.ExistsAsync(notificationType, referenceKey, cancellationToken))
        {
            return false;
        }

        var success = true;
        try
        {
            await _emailService.SendAsync(message, cancellationToken);
        }
        catch
        {
            success = false;
            throw;
        }
        finally
        {
            if (success)
            {
                await _notificationLogs.AddAsync(
                    new NotificationLog
                    {
                        NotificationType = notificationType,
                        ReferenceKey = referenceKey,
                        RecipientEmail = message.To,
                        Subject = message.Subject,
                        SentAt = DateTime.UtcNow,
                        Success = true
                    },
                    cancellationToken);
            }
        }

        return true;
    }
}
