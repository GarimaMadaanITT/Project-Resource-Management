namespace Prm.Domain.Entities;

public class ActivityTag
{
    public int Id { get; set; }
    public string TagCode { get; set; } = string.Empty;
    public string TagName { get; set; } = string.Empty;
    public string TagCategory { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
