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
    private readonly ISelectAvailableRoomService _selectAvailableRoomService;

    public AvailableRoomsController(IGetAvailableRoomsService getAvailableRoomsService, ISelectAvailableRoomService selectAvailableRoomService)
    {
        _getAvailableRoomsService = getAvailableRoomsService;
        _selectAvailableRoomService = selectAvailableRoomService;
    }
    
    [HttpGet("{hotelId}/available-rooms")]
    public async Task<IActionResult> GetAvailableRoomsAsync(int hotelId, [FromQuery] AvailableRoomsRequestDto request)
    {
        var rooms = await _getAvailableRoomsService.GetAvailableRoomsAsync(hotelId, request);
        return Ok(rooms);
    }
    
    [HttpPost("{hotelId}/available-rooms/{roomId}/selection")]
    public async Task<ActionResult<SelectedRoomResponseDto>> SelectRoom(int hotelId, int roomId, [FromBody] AvailableRoomsRequestDto request)
    {
        var result = await _selectAvailableRoomService.SelectRoomAsync(hotelId, roomId, request);
        return Ok(result);
    }
}