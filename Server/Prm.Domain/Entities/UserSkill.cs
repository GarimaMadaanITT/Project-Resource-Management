using Prm.Domain.Enums;

namespace Prm.Domain.Entities;

public class UserSkill
{
    public int UserId { get; set; }
    public int SkillId { get; set; }
    public ProficiencyLevel Proficiency { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Skill Skill { get; set; } = null!;
}
