using Microsoft.Extensions.Logging;
using Prm.Application.DTOs.Notifications;
using Prm.Application.Interfaces;

namespace Prm.Infrastructure.Email;

public class CompositeEmailService : IEmailService
{
    private readonly LoggingEmailService _loggingEmailService;
    private readonly SmtpEmailService _smtpEmailService;
    private readonly ILogger<CompositeEmailService> _logger;

    public CompositeEmailService(
        LoggingEmailService loggingEmailService,
        SmtpEmailService smtpEmailService,
        ILogger<CompositeEmailService> logger)
    {
        _loggingEmailService = loggingEmailService;
        _smtpEmailService = smtpEmailService;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        await _loggingEmailService.SendAsync(message, cancellationToken);

        try
        {
            await _smtpEmailService.SendAsync(message, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "SMTP send failed. To={To}, Subject={Subject}",
                message.To,
                message.Subject);
            throw;
        }
    }
}
