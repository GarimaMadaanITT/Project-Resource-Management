using Prm.Domain.Common;

using Prm.Domain.Enums;



namespace Prm.Domain.Entities;



public class Skill : AuditableEntity

{

    public string Name { get; set; } = string.Empty;

    public SkillCategory Category { get; set; }

    public string? CustomCategoryLabel { get; set; }



    public ICollection<UserSkill> UserSkills { get; set; } = new List<UserSkill>();

}

