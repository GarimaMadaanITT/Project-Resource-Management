namespace Prm.Infrastructure.Ai;

public class OllamaOptions
{
    public const string SectionName = "Ollama";
    private const string GeneratePathSuffix = "/api/generate";

    public string BaseUrl { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 120;

    public bool RequireApiKey { get; set; }

    public string ApiKeyHeaderName { get; set; } = "apikey";

    public static void Normalize(OllamaOptions options)
    {
        if (options is null)
        {
            throw new InvalidOperationException("Ollama options are not configured.");
        }

        options.BaseUrl = NormalizeBaseUrl(options.BaseUrl);
        options.ApiKeyHeaderName = string.IsNullOrWhiteSpace(options.ApiKeyHeaderName)
            ? "apikey"
            : options.ApiKeyHeaderName.Trim();
    }

    public static string NormalizeBaseUrl(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException(
                "Ollama:BaseUrl must be configured in appsettings.json (host root only, e.g. http://164.52.211.238).");
        }

        var trimmed = baseUrl.Trim().TrimEnd('/');

        if (trimmed.EndsWith(GeneratePathSuffix, StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[..^GeneratePathSuffix.Length].TrimEnd('/');
        }

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new InvalidOperationException("Ollama:BaseUrl is invalid after normalization.");
        }

        return trimmed;
    }
}
