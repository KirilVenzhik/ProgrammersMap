using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using ProgChecklist.Data;

namespace ProgChecklist.Web;

public static class ProgressEndpoints
{
    /// <summary>Maximum request body size of the merge endpoint, in bytes.</summary>
    public const int MaxMergeBodyBytes = 128 * 1024;

    private sealed record KeysResponse(IReadOnlyList<string> Keys);

    private sealed record MergeRequest(List<string?>? Keys);

    /// <summary>Maps /api/progress (ADR-14). Signed-in users only; mutating calls require the antiforgery header.</summary>
    public static WebApplication MapProgressEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/progress").RequireAuthorization();

        group.MapGet("", async (ClaimsPrincipal user, IProgressService progress, CancellationToken ct) =>
            TypedResults.Ok(new KeysResponse(await progress.GetKeysAsync(UserId(user), ct))));

        group.MapPut("{section}/{topic}", async (string section, string topic, ClaimsPrincipal user, IProgressService progress, CancellationToken ct) =>
                await progress.MarkAsync(UserId(user), $"{section}/{topic}", ct) ? Results.NoContent() : Results.NotFound())
            .AddEndpointFilter(ValidateAntiforgery);

        group.MapDelete("{section}/{topic}", async (string section, string topic, ClaimsPrincipal user, IProgressService progress, CancellationToken ct) =>
                await progress.UnmarkAsync(UserId(user), $"{section}/{topic}", ct) ? Results.NoContent() : Results.NotFound())
            .AddEndpointFilter(ValidateAntiforgery);

        group.MapDelete("", async (ClaimsPrincipal user, IProgressService progress, CancellationToken ct) =>
            {
                await progress.ClearAsync(UserId(user), ct);
                return Results.NoContent();
            })
            .AddEndpointFilter(ValidateAntiforgery);

        group.MapPost("merge", async (HttpContext http, ClaimsPrincipal user, IProgressService progress, CancellationToken ct) =>
            {
                var request = await ReadMergeRequestAsync(http, ct);
                if (request.Failure is { } failure)
                {
                    return failure;
                }

                var keys = request.Keys!;
                if (keys.Count > ProgressService.MaxMergeKeys)
                {
                    return Results.BadRequest();
                }

                return Results.Ok(new KeysResponse(await progress.MergeAsync(UserId(user), keys, ct)));
            })
            .AddEndpointFilter(ValidateAntiforgery);

        return app;
    }

    /// <summary>
    /// For /api requests: no-store + noindex on every response (including 401) and no status-code-page re-execution.
    /// Must run after UseStatusCodePages* and before authentication.
    /// </summary>
    public static WebApplication UseApiResponseDefaults(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                // Status code pages would re-execute API errors as Razor page requests and swallow the status.
                context.Features.Get<IStatusCodePagesFeature>()?.Enabled = false;
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers.CacheControl = "no-store";
                    context.Response.Headers["X-Robots-Tag"] = "noindex";
                    return Task.CompletedTask;
                });
            }

            await next();
        });

        return app;
    }

    private static string UserId(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Authenticated user has no id claim.");

    private static async ValueTask<object?> ValidateAntiforgery(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var antiforgery = context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return Results.BadRequest();
        }

        return await next(context);
    }

    private static async Task<(List<string?>? Keys, IResult? Failure)> ReadMergeRequestAsync(HttpContext http, CancellationToken ct)
    {
        var sizeFeature = http.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (sizeFeature is { IsReadOnly: false })
        {
            sizeFeature.MaxRequestBodySize = MaxMergeBodyBytes;
        }

        if (http.Request.ContentLength > MaxMergeBodyBytes)
        {
            return (null, Results.StatusCode(StatusCodes.Status413PayloadTooLarge));
        }

        // Bounded read so a chunked body cannot exceed the limit either.
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        int read;
        while ((read = await http.Request.Body.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + read > MaxMergeBodyBytes)
            {
                return (null, Results.StatusCode(StatusCodes.Status413PayloadTooLarge));
            }

            buffer.Write(chunk, 0, read);
        }

        buffer.Position = 0;
        try
        {
            var body = await JsonSerializer.DeserializeAsync<MergeRequest>(buffer, JsonSerializerOptions.Web, ct);
            return body?.Keys is null ? (null, Results.BadRequest()) : (body.Keys, null);
        }
        catch (JsonException)
        {
            return (null, Results.BadRequest());
        }
    }
}
