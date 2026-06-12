using Prm.Domain.Common;
using Prm.Domain.Enums;

namespace Prm.Domain.Entities;

public class User : AuditableEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Department? Department { get; set; }
    public Designation? Designation { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsTemporaryPassword { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateOnly? JoinedAt { get; set; }

    public ResourceProfile? ResourceProfile { get; set; }
    public ICollection<Project> ManagedProjects { get; set; } = new List<Project>();
    public ICollection<UserRoleAssignment> UserRoles { get; set; } = new List<UserRoleAssignment>();
    public ICollection<UserSkill> Skills { get; set; } = new List<UserSkill>();
    public ICollection<ResourceProfile> ManagedResourceProfiles { get; set; } = new List<ResourceProfile>();
}
