using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Prm.Infrastructure.Ai;

namespace Prm.Tests;

public class OllamaLlmProviderTests
{
    [Fact]
    public async Task CompleteAsync_Returns_Response_On_Success()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "model": "gemma3:12b-it-q8_0",
                      "response": "Hello there!",
                      "done": true,
                      "done_reason": "stop"
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            });

        var provider = CreateProvider(handler);
        var result = await provider.CompleteAsync(null, "System", "User", CancellationToken.None);

        Assert.Equal("Hello there!", result);
    }

    [Fact]
    public async Task CompleteAsync_Throws_On_Http_Failure()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("error")
            });

        var provider = CreateProvider(handler);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.CompleteAsync(null, "System", "User", CancellationToken.None));
    }

    [Fact]
    public async Task CompleteAsync_Throws_On_Empty_Response()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "model": "gemma3:12b-it-q8_0",
                      "response": "   ",
                      "done": true,
                      "done_reason": "stop"
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            });

        var provider = CreateProvider(handler);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.CompleteAsync(null, "System", "User", CancellationToken.None));
    }

    [Fact]
    public async Task CompleteAsync_Throws_When_Done_Is_False()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "model": "gemma3:12b-it-q8_0",
                      "response": "partial",
                      "done": false,
                      "done_reason": "length"
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            });

        var provider = CreateProvider(handler);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.CompleteAsync(null, "System", "User", CancellationToken.None));
    }

    private static OllamaLlmProvider CreateProvider(HttpMessageHandler handler)
    {
        var factory = new StubHttpClientFactory(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:11434/")
        });

        return new OllamaLlmProvider(
            factory,
            Options.Create(new OllamaOptions { Model = "gemma3:12b-it-q8_0" }),
            NullLogger<OllamaLlmProvider>.Instance);
    }

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(handler(request));
    }
}
