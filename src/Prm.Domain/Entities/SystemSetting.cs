using Prm.Domain.Enums;

namespace Prm.Domain.Entities;

public class SystemSetting
{
    public int Id { get; set; } = 1;
    public LlmProviderType LlmProvider { get; set; } = LlmProviderType.Gemini;
    public string LlmApiKey { get; set; } = string.Empty;
    public int SchedulerIntervalHours { get; set; } = 4;
    public int MaxWeeklyHours { get; set; } = 40;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
