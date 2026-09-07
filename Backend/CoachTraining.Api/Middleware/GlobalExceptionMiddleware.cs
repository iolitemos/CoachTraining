using System.Net;
using System.Text.Json;
using CoachTraining.Api.DTOs.Common;

namespace CoachTraining.Api.Middleware;

/// <summary>
/// Catches unhandled exceptions, logs enough context to trace the failure, and
/// returns the standard error response body without leaking exception details.
/// See skill.md "API Error Handling and Logging".
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception. Route: {Route} Method: {Method} UserId: {UserId}",
                context.Request.Path,
                context.Request.Method,
                context.User.Identity?.IsAuthenticated == true
                    ? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    : "anonymous");

            await WriteErrorResponseAsync(context);
        }
    }

    private static async Task WriteErrorResponseAsync(HttpContext context)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = new ApiErrorResponse("An unexpected error occurred. Please try again later.");
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
