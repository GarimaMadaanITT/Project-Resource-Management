using Prm.Domain.Common;
using Prm.Domain.Enums;

namespace Prm.Domain.Entities;

public class ResourceProfile : AuditableEntity
{
    public int UserId { get; set; }
    public int? ManagerUserId { get; set; }
    public ResourceStatus ResourceStatus { get; set; } = ResourceStatus.Bench;
    public bool TimesheetSubmissionFrozen { get; set; }
    public DateTime? TimesheetFrozenAt { get; set; }

    public User User { get; set; } = null!;
    public User? Manager { get; set; }
    public ICollection<Allocation> Allocations { get; set; } = new List<Allocation>();
    public ICollection<Timesheet> Timesheets { get; set; } = new List<Timesheet>();
}
