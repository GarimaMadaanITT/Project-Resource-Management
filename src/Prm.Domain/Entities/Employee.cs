using Prm.Domain.Common;
using Prm.Domain.Enums;

namespace Prm.Domain.Entities;

public class Employee : AuditableEntity
{
    public int UserId { get; set; }
    public string Department { get; set; } = string.Empty;
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Bench;
    public bool IsActive { get; set; } = true;
    public int? ManagerId { get; set; }

    public User User { get; set; } = null!;
    public Employee? Manager { get; set; }
    public ICollection<Employee> TeamMembers { get; set; } = new List<Employee>();
    public ICollection<EmployeeSkill> Skills { get; set; } = new List<EmployeeSkill>();
    public ICollection<Allocation> Allocations { get; set; } = new List<Allocation>();
    public ICollection<Timesheet> Timesheets { get; set; } = new List<Timesheet>();
}
