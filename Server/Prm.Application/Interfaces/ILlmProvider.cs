using Prm.Domain.Enums;

namespace Prm.Application.Interfaces;

public interface ILlmProvider
{
    LlmProviderType ProviderType { get; }

    bool RequiresApiKey { get; }

    Task<string> CompleteAsync(
        string? apiKey,
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken);
}
