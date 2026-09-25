using System.Security.Claims;
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

    public BookingsController(IModifyBookingService modifyBookingService)
    {
        _modifyBookingService = modifyBookingService;
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
}