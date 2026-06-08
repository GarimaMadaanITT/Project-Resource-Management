using Prm.Domain.Enums;
using Prm.Domain.Exceptions;

namespace Prm.Application.Validation;

public static class SettingsValidator
{
    public static LlmProviderType ParseProvider(string providerValue)
    {
        if (!Enum.TryParse<LlmProviderType>(providerValue, true, out var provider))
        {
            throw new DomainException("LLM provider must be Gemini or Groq.");
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
}
