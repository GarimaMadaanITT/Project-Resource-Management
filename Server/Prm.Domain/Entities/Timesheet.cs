using Prm.Domain.Common;
using Prm.Domain.Enums;

namespace Prm.Domain.Entities;

public class Timesheet : AuditableEntity
{
    public int ResourceProfileId { get; set; }
    public DateOnly WeekStart { get; set; }
    public TimesheetStatus Status { get; set; } = TimesheetStatus.Submitted;
    public decimal TotalHours { get; set; }

    public ResourceProfile ResourceProfile { get; set; } = null!;
    public ICollection<TimesheetEntry> Entries { get; set; } = new List<TimesheetEntry>();
}
