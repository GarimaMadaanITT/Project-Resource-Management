using Prm.Application.Interfaces;
using Prm.Domain.Enums;
using Prm.Domain.Exceptions;

namespace Prm.Infrastructure.Ai;

public class LlmProviderRegistry : ILlmProviderRegistry
{
    private readonly IReadOnlyDictionary<LlmProviderType, ILlmProvider> _providers;

    public LlmProviderRegistry(IEnumerable<ILlmProvider> providers)
    {
        _providers = providers.ToDictionary(provider => provider.ProviderType);
    }

    public ILlmProvider GetProvider(LlmProviderType providerType)
    {
        if (!_providers.TryGetValue(providerType, out var provider))
        {
            throw new DomainException($"LLM provider '{providerType}' is not registered.");
        }

        return provider;
    }
}
