using Microsoft.Extensions.Logging;
using Prm.Application.Interfaces;
using Prm.Domain.Enums;

namespace Prm.Infrastructure.Ai;

public class LlmCompletionService : ILlmCompletionService
{
    private readonly ISystemSettingsRepository _settings;
    private readonly ILlmProviderRegistry _providerRegistry;
    private readonly DeterministicLlmProvider _deterministic;
    private readonly ILogger<LlmCompletionService> _logger;

    public LlmCompletionService(
        ISystemSettingsRepository settings,
        ILlmProviderRegistry providerRegistry,
        DeterministicLlmProvider deterministic,
        ILogger<LlmCompletionService> logger)
    {
        _settings = settings;
        _providerRegistry = providerRegistry;
        _deterministic = deterministic;
        _logger = logger;
    }

    public async Task<LlmCompletionResult> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        var settings = await _settings.GetAsync(cancellationToken);
        var provider = _providerRegistry.GetProvider(settings.LlmProvider);

        if (provider.RequiresApiKey && string.IsNullOrWhiteSpace(settings.LlmApiKey))
        {
            _logger.LogInformation(
                "LLM API key is not configured for provider {Provider}. Using deterministic fallback.",
                settings.LlmProvider);
            return await _deterministic.CompleteFallbackAsync(systemPrompt, userPrompt, cancellationToken);
        }

        try
        {
            var text = await provider.CompleteAsync(
                settings.LlmApiKey,
                systemPrompt,
                userPrompt,
                cancellationToken);

            return new LlmCompletionResult(text, UsedFallbackProvider: false);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "External LLM call failed for provider {Provider}. Falling back to deterministic provider.",
                settings.LlmProvider);
            return await _deterministic.CompleteFallbackAsync(systemPrompt, userPrompt, cancellationToken);
        }
    }
}
