using System.Diagnostics;
using System.Security.Claims;

namespace HotelBooking.API.Middleware;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
            var statusCode = context.Response.StatusCode;

            if (statusCode >= StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(
                    "HTTP {Method} {Path} returned {StatusCode} in {ElapsedMilliseconds} ms for user {UserId}. TraceId: {TraceId}",
                    context.Request.Method,
                    context.Request.Path,
                    statusCode,
                    stopwatch.ElapsedMilliseconds,
                    userId,
                    context.TraceIdentifier);
            }
            else if (statusCode >= StatusCodes.Status400BadRequest)
            {
                _logger.LogWarning(
                    "HTTP {Method} {Path} returned {StatusCode} in {ElapsedMilliseconds} ms for user {UserId}. TraceId: {TraceId}",
                    context.Request.Method,
                    context.Request.Path,
                    statusCode,
                    stopwatch.ElapsedMilliseconds,
                    userId,
                    context.TraceIdentifier);
            }
            else
            {
                _logger.LogInformation(
                    "HTTP {Method} {Path} returned {StatusCode} in {ElapsedMilliseconds} ms for user {UserId}. TraceId: {TraceId}",
                    context.Request.Method,
                    context.Request.Path,
                    statusCode,
                    stopwatch.ElapsedMilliseconds,
                    userId,
                    context.TraceIdentifier);
            }
        }
    }
}
