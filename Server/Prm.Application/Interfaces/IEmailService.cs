using Prm.Application.DTOs.Notifications;

namespace Prm.Application.Interfaces;

public interface IEmailService
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
