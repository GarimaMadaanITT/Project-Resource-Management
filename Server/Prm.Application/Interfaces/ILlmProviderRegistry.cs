using Prm.Domain.Enums;

namespace Prm.Application.Interfaces;

public interface ILlmProviderRegistry
{
    ILlmProvider GetProvider(LlmProviderType providerType);
}
