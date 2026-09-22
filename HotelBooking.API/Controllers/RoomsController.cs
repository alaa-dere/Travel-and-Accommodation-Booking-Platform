using HotelBooking.Application.Rooms.Create;
using HotelBooking.Application.Rooms.Dtos;
using HotelBooking.Application.Rooms.Retrive;
using HotelBooking.Application.Rooms.Update;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles =  "Admin")]
public class RoomsController : ControllerBase
{
    private readonly IGetAllRoomsService _getAllRoomsService;
    private readonly ICreateRoomService _createRoomService;
    private readonly IUpdateRoomService _updateRoomlService;
    public RoomsController(ICreateRoomService  createRoomService, IGetAllRoomsService  getAllRoomsService, IUpdateRoomService  updateRoomlService)
    {
        _createRoomService = createRoomService;
        _getAllRoomsService = getAllRoomsService;
        _updateRoomlService = updateRoomlService;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAsync([FromQuery] string? search)
    {
        var rooms = await _getAllRoomsService.GetAllRoomsAsync(search);
        return Ok(rooms);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateRoomAsync(RoomRequestDto request)
    {
        var room = await _createRoomService.CreateRoomAsync(request);
        return Created("api/rooms", room);
    }
    
    [HttpPut("{roomId}")]
    public async Task<IActionResult> PutAsync(int roomId, RoomRequestDto request)
    {
        var room = await _updateRoomlService.UpdateRoomAsync(roomId, request);
        return Ok(room);
    }
}