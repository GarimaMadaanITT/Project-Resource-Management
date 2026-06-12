using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Prm.Application.Interfaces;
using Prm.Domain.Enums;

namespace Prm.Infrastructure.Ai;

public class GroqLlmProvider : ILlmProvider
{
    private const string Model = "llama-3.3-70b-versatile";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GroqLlmProvider> _logger;

    public GroqLlmProvider(IHttpClientFactory httpClientFactory, ILogger<GroqLlmProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public LlmProviderType ProviderType => LlmProviderType.Groq;

    public bool RequiresApiKey => true;

    public async Task<string> CompleteAsync(
        string? apiKey,
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Groq API key is required.");
        }

        var client = _httpClientFactory.CreateClient("Groq");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var payload = new
        {
            model = Model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.2
        };

        using var response = await client.PostAsJsonAsync("openai/v1/chat/completions", payload, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Groq API call failed with status {StatusCode}: {Body}", response.StatusCode, body);
            throw new InvalidOperationException("Groq API call failed.");
        }

        using var document = JsonDocument.Parse(body);
        var text = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("Groq returned an empty response: {Body}", body);
            throw new InvalidOperationException("Groq API returned an empty response.");
        }

        return text;
    }
}
