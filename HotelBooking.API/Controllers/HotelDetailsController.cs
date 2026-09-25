using System.Security.Claims;
using HotelBooking.Application.HotelDetails;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.API.Controllers;

[ApiController]
[Route("api/hotels")]
[Authorize(Roles = "Customer")]
public class HotelDetailsController : ControllerBase
{
    private readonly IGetHotelDetailsService _hotelDetailsService;

    public HotelDetailsController(IGetHotelDetailsService hotelDetailsService)
    {
        _hotelDetailsService = hotelDetailsService;
    }

    [HttpGet("{hotelId:int}")]
    public async Task<IActionResult> GetHotelDetailsAsync(int hotelId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }
        var hotel = await _hotelDetailsService.GetHotelDetailsAsync(hotelId, userId);
        return Ok(hotel);
    }
}