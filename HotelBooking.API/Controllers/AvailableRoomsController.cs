using HotelBooking.Application.AvailableRooms;
using HotelBooking.Application.AvailableRooms.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.API.Controllers;

[ApiController]
[Route("api/hotels")]
[Authorize(Roles = "Customer")]
public class AvailableRoomsController : ControllerBase
{
    private readonly IGetAvailableRoomsService _getAvailableRoomsService;

    public AvailableRoomsController(IGetAvailableRoomsService getAvailableRoomsService)
    {
        _getAvailableRoomsService = getAvailableRoomsService;
    }
    
    [HttpGet("{hotelId}/available-rooms")]
    public async Task<IActionResult> GetAvailableRoomsAsync(int hotelId, [FromQuery] AvailableRoomsRequestDto request)
    {
        var rooms = await _getAvailableRoomsService.GetAvailableRoomsAsync(hotelId, request);
        return Ok(rooms);
    }
}