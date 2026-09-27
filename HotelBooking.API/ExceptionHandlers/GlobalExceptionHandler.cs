using HotelBooking.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace HotelBooking.API.ExceptionHandlers;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var statusCode = exception switch
        {
            ConflictException => StatusCodes.Status409Conflict,
            UnauthorizedException => StatusCodes.Status401Unauthorized,
            NotFoundException => StatusCodes.Status404NotFound,
            BadRequestException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "Unhandled exception while processing {Method} {Path}. TraceId: {TraceId}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                httpContext.TraceIdentifier);
        }
        else
        {
            // Avoid logging the message because application errors can contain user-provided data.
            _logger.LogWarning(
                "Request rejected with {ExceptionType} and status {StatusCode}. TraceId: {TraceId}",
                exception.GetType().Name,
                statusCode,
                httpContext.TraceIdentifier);
        }

        httpContext.Response.StatusCode = statusCode;
        var responseMessage = statusCode == StatusCodes.Status500InternalServerError
            ? "An unexpected error occurred."
            : exception.Message;

        await httpContext.Response.WriteAsJsonAsync(
            new
            {
                message = responseMessage,
                traceId = httpContext.TraceIdentifier
            },
            cancellationToken);

        return true;
    }
}
