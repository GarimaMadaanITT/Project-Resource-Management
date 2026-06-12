namespace Prm.Domain.Entities;

public class AiRequestLog
{
    public int Id { get; set; }
    public int RequestedByUserId { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public string? Prompt { get; set; }
    public string? ResponseSummary { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User RequestedByUser { get; set; } = null!;
}
