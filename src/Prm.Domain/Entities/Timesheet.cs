using Prm.Domain.Common;
using Prm.Domain.Enums;

namespace Prm.Domain.Entities;

public class Timesheet : AuditableEntity
{
    public int EmployeeId { get; set; }
    public DateOnly WeekStart { get; set; }
    public TimesheetStatus Status { get; set; } = TimesheetStatus.Submitted;
    public decimal TotalHours { get; set; }

    public Employee Employee { get; set; } = null!;
    public ICollection<TimesheetEntry> Entries { get; set; } = new List<TimesheetEntry>();
}
