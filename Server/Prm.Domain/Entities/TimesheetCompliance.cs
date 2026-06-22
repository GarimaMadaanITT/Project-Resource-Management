using Prm.Domain.Common;
using Prm.Domain.Enums;

namespace Prm.Domain.Entities;

public class TimesheetCompliance : AuditableEntity
{
    public int ResourceProfileId { get; set; }
    public DateOnly WeekStart { get; set; }
    public TimesheetComplianceStatus Status { get; set; } = TimesheetComplianceStatus.Pending;
    public int ReminderCount { get; set; }
    public DateTime? Reminder1SentAt { get; set; }
    public DateTime? Reminder2SentAt { get; set; }
    public DateTime? FreezeNotifiedAt { get; set; }

    public ResourceProfile ResourceProfile { get; set; } = null!;
}
