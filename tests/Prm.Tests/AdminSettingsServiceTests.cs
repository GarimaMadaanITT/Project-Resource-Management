using Moq;
using Prm.Application.DTOs.Admin;
using Prm.Application.Interfaces;
using Prm.Application.Services.Admin;
using Prm.Domain.Entities;
using Prm.Domain.Enums;
using Prm.Domain.Exceptions;

namespace Prm.Tests;

public class AdminSettingsServiceTests
{
    [Fact]
    public async Task UpdateAsync_Persists_LlmProvider_And_ApiKey()
    {
        var settings = new SystemSetting
        {
            LlmProvider = LlmProviderType.Gemini,
            LlmApiKey = string.Empty
        };

        var repository = new Mock<ISystemSettingsRepository>();
        repository.Setup(r => r.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);
        repository.Setup(r => r.UpdateAsync(It.IsAny<SystemSetting>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new AdminSettingsService(repository.Object);
        var result = await service.UpdateAsync(
            new UpdateSystemSettingsRequest("Gemini", "test-api-key-value", null, null));

        Assert.Equal("Gemini", result.LlmProvider);
        Assert.Contains("alue", result.LlmApiKeyMasked);
        Assert.Equal("test-api-key-value", settings.LlmApiKey);
        repository.Verify(r => r.UpdateAsync(settings, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_Throws_When_ApiKey_Is_Empty()
    {
        var repository = new Mock<ISystemSettingsRepository>();
        repository.Setup(r => r.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SystemSetting());

        var service = new AdminSettingsService(repository.Object);

        await Assert.ThrowsAsync<DomainException>(() =>
            service.UpdateAsync(new UpdateSystemSettingsRequest(null, "   ", null, null)));
    }
}
