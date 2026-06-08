namespace Prm.Application.DTOs.Admin;

public record SystemSettingsDto(
    string LlmProvider,
    string LlmApiKeyMasked,
    int SchedulerIntervalHours,
    int MaxWeeklyHours);

public record UpdateSystemSettingsRequest(
    string? LlmProvider,
    string? LlmApiKey,
    int? SchedulerIntervalHours,
    int? MaxWeeklyHours);
