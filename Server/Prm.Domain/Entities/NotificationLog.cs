using Prm.Domain.Common;
using Prm.Domain.Enums;

namespace Prm.Domain.Entities;

public class NotificationLog : AuditableEntity
{
    public NotificationType NotificationType { get; set; }
    public string ReferenceKey { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public bool Success { get; set; } = true;
}
