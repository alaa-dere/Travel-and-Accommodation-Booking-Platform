using HotelBooking.Application.Rooms.Create;
using HotelBooking.Application.Rooms.Delete;
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
    private readonly IUpdateRoomService _updateRoomService;
    private readonly IChangeRoomStatusService _changeRoomStatusService;
    private readonly IChangeRoomOperationalAvailabilityService _changeRoomOperationalAvailabilityService;
    public RoomsController(ICreateRoomService createRoomService, IGetAllRoomsService getAllRoomsService, IUpdateRoomService updateRoomlService, IChangeRoomStatusService  changeRoomStatusService , IChangeRoomOperationalAvailabilityService changeRoomOperationalAvailabilityService)
    {
        _createRoomService = createRoomService;
        _getAllRoomsService = getAllRoomsService;
        _updateRoomService = updateRoomlService;
        _changeRoomStatusService = changeRoomStatusService;
        _changeRoomOperationalAvailabilityService = changeRoomOperationalAvailabilityService;
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
    
    [HttpPut("{roomId:int}")]
    public async Task<IActionResult> PutAsync(int roomId, RoomRequestDto request)
    {
        var room = await _updateRoomService.UpdateRoomAsync(roomId, request);
        return Ok(room);
    }
    
    [HttpPatch("{roomId:int}/status")]
    public async Task<IActionResult> ChangeStatusAsync(int roomId, ChangeRoomStatusRequest request)
    { 
        await _changeRoomStatusService.ChangeRoomStatusAsync(roomId, request.IsActive);
        return NoContent();
    }
    
    [HttpPatch("{roomId:int}/operational-availability")]
    public async Task<IActionResult> ChangeOperationalAvailabilityAsync(int roomId, ChangeRoomOperationalAvailabilityRequest request)
    {
        await _changeRoomOperationalAvailabilityService.ChangeOperationalAvailabilityAsync(roomId, request.IsOperationallyAvailable);
        return NoContent();
    }
}