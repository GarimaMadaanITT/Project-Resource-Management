using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prm.Application.Interfaces;
using Prm.Domain.Enums;

namespace Prm.Infrastructure.Ai;

public class OllamaLlmProvider : ILlmProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OllamaOptions _options;
    private readonly ILogger<OllamaLlmProvider> _logger;

    public OllamaLlmProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<OllamaOptions> options,
        ILogger<OllamaLlmProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public LlmProviderType ProviderType => LlmProviderType.Ollama;

    public bool RequiresApiKey => _options.RequireApiKey;

    public async Task<string> CompleteAsync(
        string? apiKey,
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        if (_options.RequireApiKey && string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Ollama API key is required. Configure it in Admin system settings.");
        }

        var client = _httpClientFactory.CreateClient("Ollama");
        var prompt = $"{systemPrompt.Trim()}\n\n{userPrompt.Trim()}";
        var payload = new OllamaGenerateRequest(_options.Model, prompt, Stream: false);

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/generate")
        {
            Content = JsonContent.Create(payload)
        };

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.TryAddWithoutValidation(_options.ApiKeyHeaderName, apiKey.Trim());
        }

        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Ollama API call failed with status {StatusCode}: {Body}",
                response.StatusCode,
                body);
            throw new InvalidOperationException("Ollama API call failed.");
        }

        OllamaGenerateResponse? result;
        try
        {
            result = System.Text.Json.JsonSerializer.Deserialize<OllamaGenerateResponse>(body);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Ollama returned invalid JSON: {Body}", body);
            throw new InvalidOperationException("Ollama API returned an invalid response.");
        }

        if (result is null)
        {
            _logger.LogWarning("Ollama returned empty deserialized response: {Body}", body);
            throw new InvalidOperationException("Ollama API returned an invalid response.");
        }

        if (!result.Done)
        {
            _logger.LogWarning(
                "Ollama generation incomplete. DoneReason={DoneReason}, Body={Body}",
                result.DoneReason,
                body);
            throw new InvalidOperationException("Ollama API did not complete generation.");
        }

        if (string.IsNullOrWhiteSpace(result.Response))
        {
            _logger.LogWarning("Ollama returned an empty response body: {Body}", body);
            throw new InvalidOperationException("Ollama API returned an empty response.");
        }

        return result.Response.Trim();
    }

    private sealed record OllamaGenerateRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("prompt")] string Prompt,
        [property: JsonPropertyName("stream")] bool Stream);

    private sealed record OllamaGenerateResponse(
        [property: JsonPropertyName("model")] string? Model,
        [property: JsonPropertyName("response")] string? Response,
        [property: JsonPropertyName("done")] bool Done,
        [property: JsonPropertyName("done_reason")] string? DoneReason);
}
