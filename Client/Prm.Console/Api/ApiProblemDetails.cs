using System.Net.Http.Json;
using System.Text.Json;

namespace Prm.Client.Api;

public sealed class ApiProblemDetails
{
    public string? Detail { get; init; }

    public static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ApiProblemDetails>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken);
            if (!string.IsNullOrWhiteSpace(problem?.Detail))
            {
                return problem.Detail;
            }
        }
        catch
        {
            // ignored
        }

        return $"Request failed ({(int)response.StatusCode}).";
    }
}

public sealed class ApiRequestException : Exception
{
    public ApiRequestException(string message) : base(message)
    {
    }
}

public sealed class SessionExpiredException : Exception
{
    public SessionExpiredException(string message) : base(message)
    {
    }
}
