using Prm.Application.Common;
using Prm.Application.DTOs.Admin;
using Prm.Application.Interfaces;
using Prm.Application.Validation;

namespace Prm.Application.Services.Admin;

public class AdminSettingsService : IAdminSettingsService
{
    private readonly ISystemSettingsRepository _settings;

    public AdminSettingsService(ISystemSettingsRepository settings)
    {
        _settings = settings;
    }

    public async Task<SystemSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settings.GetAsync(cancellationToken);
        return Map(settings);
    }

    public async Task<SystemSettingsDto> UpdateAsync(UpdateSystemSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var settings = await _settings.GetAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.LlmProvider))
        {
            settings.LlmProvider = SettingsValidator.ParseProvider(request.LlmProvider);
        }

        if (request.LlmApiKey is not null)
        {
            settings.LlmApiKey = SettingsValidator.NormalizeApiKey(request.LlmApiKey);
        }

        if (request.SchedulerIntervalHours.HasValue)
        {
            SettingsValidator.EnsurePositive(request.SchedulerIntervalHours.Value, "Scheduler interval");
            settings.SchedulerIntervalHours = request.SchedulerIntervalHours.Value;
        }

        if (request.MaxWeeklyHours.HasValue)
        {
            SettingsValidator.EnsurePositive(request.MaxWeeklyHours.Value, "Max weekly hours");
            settings.MaxWeeklyHours = request.MaxWeeklyHours.Value;
        }

        settings.UpdatedAt = DateTime.UtcNow;
        await _settings.UpdateAsync(settings, cancellationToken);
        return Map(settings);
    }

    private static SystemSettingsDto Map(Domain.Entities.SystemSetting settings) =>
        new(
            settings.LlmProvider.ToString(),
            ApiKeyMasker.Mask(settings.LlmApiKey),
            settings.SchedulerIntervalHours,
            settings.MaxWeeklyHours);
}
