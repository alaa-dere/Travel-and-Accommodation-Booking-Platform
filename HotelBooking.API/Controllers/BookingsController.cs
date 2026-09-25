using System.Security.Claims;
using HotelBooking.Application.Bookings.Cancel;
using HotelBooking.Application.Bookings.Dtos;
using HotelBooking.Application.Bookings.Modify;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.API.Controllers;

[ApiController]
[Route("api/bookings")]
[Authorize(Roles = "Customer")]
public class BookingsController : ControllerBase
{
    private readonly IModifyBookingService _modifyBookingService;
    private readonly ICancelBookingService _cancelBookingService;

    public BookingsController(IModifyBookingService modifyBookingService, ICancelBookingService cancelBookingService)
    {
        _modifyBookingService = modifyBookingService;
        _cancelBookingService = cancelBookingService;
    }

    [HttpPut("{bookingId:int}")]
    public async Task<IActionResult> ModifyBooking(int bookingId, [FromBody] ModifyBookingRequestDto request)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        await _modifyBookingService.ModifyAsync(bookingId, userId, request);
        return NoContent();
    }
    
    [HttpDelete("{bookingId:int}")]
    public async Task<IActionResult> CancelBooking(int bookingId)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }
        await _cancelBookingService.CancelAsync(bookingId, userId);
        return NoContent();
    }
}