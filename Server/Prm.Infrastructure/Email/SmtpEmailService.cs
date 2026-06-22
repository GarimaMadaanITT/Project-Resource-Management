using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Prm.Application.DTOs.Notifications;
using Prm.Application.Interfaces;

namespace Prm.Infrastructure.Email;

public class SmtpEmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailOptions> options, ILogger<SmtpEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (!_options.Smtp.IsConfigured)
        {
            _logger.LogWarning("SMTP is not configured. Skipping SMTP send for To={To}, Subject={Subject}", message.To, message.Subject);
            return;
        }

        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        mimeMessage.To.Add(MailboxAddress.Parse(message.To));
        mimeMessage.Subject = message.Subject;
        mimeMessage.Body = new TextPart("plain") { Text = message.PlainTextBody };

        using var client = new SmtpClient();
        var secureSocketOptions = _options.Smtp.UseSsl
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTlsWhenAvailable;

        await client.ConnectAsync(_options.Smtp.Host, _options.Smtp.Port, secureSocketOptions, cancellationToken);
        await client.AuthenticateAsync(_options.Smtp.Username, _options.Smtp.Password, cancellationToken);
        await client.SendAsync(mimeMessage, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        _logger.LogInformation("SMTP email sent. To={To}, Subject={Subject}", message.To, message.Subject);
    }
}
