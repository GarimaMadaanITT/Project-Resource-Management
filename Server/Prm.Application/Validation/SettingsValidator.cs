using Prm.Domain.Enums;
using Prm.Domain.Exceptions;

namespace Prm.Application.Validation;

public static class SettingsValidator
{
    public static LlmProviderType ParseProvider(string providerValue)
    {
        if (!Enum.TryParse<LlmProviderType>(providerValue, true, out var provider))
        {
            throw new DomainException("LLM provider must be Gemini, Groq, or Ollama.");
        }

        return provider;
    }

    public static void EnsurePositive(int value, string fieldName)
    {
        if (value <= 0)
        {
            throw new DomainException($"{fieldName} must be greater than zero.");
        }
    }

    public static string NormalizeApiKey(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new DomainException("LLM API key cannot be empty.");
        }

        return apiKey.Trim();
    }
}
