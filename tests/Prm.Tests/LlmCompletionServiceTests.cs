using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Prm.Application.Interfaces;
using Prm.Domain.Entities;
using Prm.Domain.Enums;
using Prm.Infrastructure.Ai;

namespace Prm.Tests;

public class LlmCompletionServiceTests
{
    [Fact]
    public async Task CompleteAsync_Uses_Ollama_Without_Api_Key()
    {
        var settings = new SystemSetting { LlmProvider = LlmProviderType.Ollama, LlmApiKey = string.Empty };
        var ollama = new Mock<ILlmProvider>();
        ollama.SetupGet(p => p.ProviderType).Returns(LlmProviderType.Ollama);
        ollama.SetupGet(p => p.RequiresApiKey).Returns(false);
        ollama.Setup(p => p.CompleteAsync(
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("ollama-response");

        var service = CreateService(settings, ollama.Object);

        var result = await service.CompleteAsync("sys", "user");

        Assert.False(result.UsedFallbackProvider);
        Assert.Equal("ollama-response", result.Text);
    }

    [Fact]
    public async Task CompleteAsync_Falls_Back_When_Gemini_Api_Key_Missing()
    {
        var settings = new SystemSetting { LlmProvider = LlmProviderType.Gemini, LlmApiKey = string.Empty };
        var gemini = new Mock<ILlmProvider>();
        gemini.SetupGet(p => p.ProviderType).Returns(LlmProviderType.Gemini);
        gemini.SetupGet(p => p.RequiresApiKey).Returns(true);

        var service = CreateService(settings, gemini.Object);

        var result = await service.CompleteAsync(
            "You are a resource planning assistant for an IT services company.",
            "Requirement: backend");

        Assert.True(result.UsedFallbackProvider);
        Assert.Contains("matches", result.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CompleteAsync_Falls_Back_When_Selected_Provider_Throws()
    {
        var settings = new SystemSetting { LlmProvider = LlmProviderType.Ollama, LlmApiKey = string.Empty };
        var ollama = new Mock<ILlmProvider>();
        ollama.SetupGet(p => p.ProviderType).Returns(LlmProviderType.Ollama);
        ollama.SetupGet(p => p.RequiresApiKey).Returns(false);
        ollama.Setup(p => p.CompleteAsync(
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Ollama API call failed."));

        var service = CreateService(settings, ollama.Object);

        var result = await service.CompleteAsync(
            "You are a delivery manager assistant.",
            "Project: Alpha\nRisk flags:\n- No critical risk flags");

        Assert.True(result.UsedFallbackProvider);
        Assert.Contains("Alpha", result.Text);
    }

    private static LlmCompletionService CreateService(SystemSetting settings, ILlmProvider provider)
    {
        var settingsRepo = new Mock<ISystemSettingsRepository>();
        settingsRepo.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        var registry = new LlmProviderRegistry(new[] { provider });

        return new LlmCompletionService(
            settingsRepo.Object,
            registry,
            new DeterministicLlmProvider(NullLogger<DeterministicLlmProvider>.Instance),
            NullLogger<LlmCompletionService>.Instance);
    }
}
