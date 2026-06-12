namespace Prm.Application.Interfaces;

public record LlmCompletionResult(string Text, bool UsedFallbackProvider);

public interface ILlmCompletionService
{
    Task<LlmCompletionResult> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);
}
