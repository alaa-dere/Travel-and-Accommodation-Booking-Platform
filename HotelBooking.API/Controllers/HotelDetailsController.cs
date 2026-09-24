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

    [HttpGet("{hotelId}")]
    public async Task<IActionResult> GetHotelDetailsAsync(int hotelId)
    {
        var hotel = await _hotelDetailsService.GetHotelDetailsAsync(hotelId);
        return Ok(hotel);
    }
}