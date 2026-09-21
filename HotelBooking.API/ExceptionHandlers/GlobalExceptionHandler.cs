using HotelBooking.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace HotelBooking.API.ExceptionHandlers;

public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is ConflictException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
            await httpContext.Response.WriteAsJsonAsync(new { message = exception.Message }, cancellationToken);
            return true;
        }

        if (exception is UnauthorizedException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await httpContext.Response.WriteAsJsonAsync(new { message = exception.Message }, cancellationToken);
            return true;
        }

        return false;
    }
}