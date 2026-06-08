using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Prm.Application.Common;

namespace Prm.Api.Middleware;

public class ForcePasswordChangeMiddleware
{
    private static readonly PathString[] AllowedPaths =
    [
        new("/health"),
        new("/api/auth/login"),
        new("/api/auth/change-password")
    ];

    private readonly RequestDelegate _next;

    public ForcePasswordChangeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsAllowedPath(context.Request.Path) || context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var forceChange = context.User.FindFirstValue(AuthConstants.ForcePasswordChangeClaim);
        if (string.Equals(forceChange, "true", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                type = "https://tools.ietf.org/html/rfc7807",
                title = "Password change required",
                status = 403,
                detail = "You must change your password before accessing this resource.",
                code = "FORCE_PASSWORD_CHANGE"
            }));
            return;
        }

        await _next(context);
    }

    private static bool IsAllowedPath(PathString path)
    {
        foreach (var allowed in AllowedPaths)
        {
            if (path.StartsWithSegments(allowed, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase);
    }
}
