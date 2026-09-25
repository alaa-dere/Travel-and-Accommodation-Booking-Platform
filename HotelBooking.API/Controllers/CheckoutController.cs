using System.Security.Claims;
using HotelBooking.Application.Bookings;
using HotelBooking.Application.Bookings.Dtos;
using HotelBooking.Application.Checkout.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.API.Controllers;

[ApiController]
[Route("api/checkout")]
[Authorize(Roles = "Customer")]
public class CheckoutController : ControllerBase
{
    private readonly ICreateBookingsService _createBookingsService;

    public CheckoutController(ICreateBookingsService createBookingsService)
    {
        _createBookingsService = createBookingsService;
    }

    [HttpPost]
    public async Task<ActionResult<BookingCreationResultDto>> CompleteCheckout([FromBody] CompleteCheckoutRequestDto request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }
        
        var result = await _createBookingsService.CreateBookingsAsync(userId, request.SpecialRequests);
        return Ok(result);
    }
}