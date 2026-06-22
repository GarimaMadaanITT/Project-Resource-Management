using Prm.Infrastructure.Ai;

namespace Prm.Tests;

public class OllamaOptionsTests
{
    [Fact]
    public void NormalizeBaseUrl_Uses_Configured_Host_Root()
    {
        var normalized = OllamaOptions.NormalizeBaseUrl("http://164.52.211.238");

        Assert.Equal("http://164.52.211.238", normalized);
    }

    [Fact]
    public void NormalizeBaseUrl_Strips_Generate_Path_Suffix()
    {
        var normalized = OllamaOptions.NormalizeBaseUrl("http://164.52.211.238/api/generate");

        Assert.Equal("http://164.52.211.238", normalized);
    }

    [Fact]
    public void NormalizeBaseUrl_Throws_When_Missing()
    {
        Assert.Throws<InvalidOperationException>(() => OllamaOptions.NormalizeBaseUrl(null));
        Assert.Throws<InvalidOperationException>(() => OllamaOptions.NormalizeBaseUrl("   "));
    }
}
