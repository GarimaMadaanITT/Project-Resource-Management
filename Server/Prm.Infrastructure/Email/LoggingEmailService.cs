using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prm.Application.DTOs.Notifications;
using Prm.Application.Interfaces;

namespace Prm.Infrastructure.Email;

public class LoggingEmailService : IEmailService
{
    private readonly ILogger<LoggingEmailService> _logger;
    private readonly EmailOptions _options;

    public LoggingEmailService(ILogger<LoggingEmailService> logger, IOptions<EmailOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (!_options.EnableLogging)
        {
            return Task.CompletedTask;
        }

        _logger.LogInformation(
            """
            Email notification.
            From: {FromName} <{FromAddress}>
            To: {To}
            Subject: {Subject}
            Body:
            {Body}
            """,
            _options.FromName,
            _options.FromAddress,
            message.To,
            message.Subject,
            message.PlainTextBody);

        return Task.CompletedTask;
    }
}
