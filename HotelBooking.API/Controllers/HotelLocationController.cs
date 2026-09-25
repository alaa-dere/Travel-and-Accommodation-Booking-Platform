using HotelBooking.Application.HotelLocations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.API.Controllers;

[ApiController]
[Route("api/hotels")]
[Authorize(Roles = "Customer")]
public class HotelLocationController : ControllerBase
{
    private readonly IGetHotelLocationService _getHotelLocationService;

    public HotelLocationController(IGetHotelLocationService getHotelLocationService)
    {
        _getHotelLocationService = getHotelLocationService;
    }

    [HttpGet("{hotelId}/location")]
    public async Task<IActionResult> GetHotelLocationAsync(int hotelId)
    {
        var result = await _getHotelLocationService.GetHotelLocationAsync(hotelId);
        return Ok(result);
    }
}