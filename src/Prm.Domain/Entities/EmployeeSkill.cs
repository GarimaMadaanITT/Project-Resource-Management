using Prm.Domain.Enums;

namespace Prm.Domain.Entities;

public class EmployeeSkill
{
    public int EmployeeId { get; set; }
    public int SkillId { get; set; }
    public ProficiencyLevel Proficiency { get; set; }

    public Employee Employee { get; set; } = null!;
    public Skill Skill { get; set; } = null!;
}
