using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Prm.Application.Interfaces;
using Prm.Domain.Enums;

namespace Prm.Infrastructure.Ai;

public class GeminiLlmProvider : ILlmProvider
{
    private const string Model = "gemini-2.0-flash";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GeminiLlmProvider> _logger;

    public GeminiLlmProvider(IHttpClientFactory httpClientFactory, ILogger<GeminiLlmProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public LlmProviderType ProviderType => LlmProviderType.Gemini;

    public bool RequiresApiKey => true;

    public async Task<string> CompleteAsync(
        string? apiKey,
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Gemini API key is required.");
        }

        var client = _httpClientFactory.CreateClient("Gemini");
        var url = $"v1beta/models/{Model}:generateContent?key={Uri.EscapeDataString(apiKey)}";
        var payload = new
        {
            systemInstruction = new { parts = new[] { new { text = systemPrompt } } },
            contents = new[]
            {
                new { role = "user", parts = new[] { new { text = userPrompt } } }
            }
        };

        using var response = await client.PostAsJsonAsync(url, payload, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Gemini API call failed with status {StatusCode}: {Body}", response.StatusCode, body);
            throw new InvalidOperationException("Gemini API call failed.");
        }

        using var document = JsonDocument.Parse(body);
        var text = document.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("Gemini returned an empty response: {Body}", body);
            throw new InvalidOperationException("Gemini API returned an empty response.");
        }

        return text;
    }
}
