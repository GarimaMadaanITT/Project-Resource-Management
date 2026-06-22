namespace Prm.Domain.Entities;

public class UserRoleAssignment
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public bool IsPrimary { get; set; } = true;
    public int? AssignedByUserId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
    public User? AssignedByUser { get; set; }
}
