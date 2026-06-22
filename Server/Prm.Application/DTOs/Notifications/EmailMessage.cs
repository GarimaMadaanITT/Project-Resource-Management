namespace Prm.Application.DTOs.Notifications;

public record EmailMessage(
    string To,
    string Subject,
    string PlainTextBody);
