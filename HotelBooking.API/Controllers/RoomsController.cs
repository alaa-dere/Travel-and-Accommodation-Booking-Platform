using HotelBooking.Application.Rooms.Create;
using HotelBooking.Application.Rooms.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles =  "Admin")]
public class RoomsController : ControllerBase
{
    //private readonly IGetAllRoomsService _getAllRoomsService;
    private readonly ICreateRoomService _createRoomService;
    public RoomsController(ICreateRoomService  createRoomService)
    {
        _createRoomService = createRoomService;
    }
    
    // [HttpGet]
    // public async Task<IActionResult> GetAsync([FromQuery] string? search)
    // {
    //     var rooms = await _getAllRoomsService.GetAllRoomsAsync(search);
    //     return Ok(rooms);
    // }
    
    [HttpPost]
    public async Task<IActionResult> CreateRoomAsync(RoomRequestDto request)
    {
        var room = await _createRoomService.CreateRoomAsync(request);
        return Created("api/rooms", room);
    }
}