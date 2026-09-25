using HotelBooking.Application.Rooms.Create;
using HotelBooking.Application.Rooms.Delete;
using HotelBooking.Application.Rooms.Dtos;
using HotelBooking.Application.Rooms.Retrive;
using HotelBooking.Application.Rooms.Update;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelBooking.Application.Rooms.Images;

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
    private readonly IAddRoomImageService _addRoomImageService;
    private readonly IDeleteRoomImageService _deleteRoomImageService;
    public RoomsController(
        ICreateRoomService createRoomService, 
        IGetAllRoomsService getAllRoomsService, 
        IUpdateRoomService updateRoomlService, 
        IChangeRoomStatusService  changeRoomStatusService , 
        IChangeRoomOperationalAvailabilityService changeRoomOperationalAvailabilityService,
        IAddRoomImageService addRoomImageService,
        IDeleteRoomImageService deleteRoomImageService
        )
    {
        _createRoomService = createRoomService;
        _getAllRoomsService = getAllRoomsService;
        _updateRoomService = updateRoomlService;
        _changeRoomStatusService = changeRoomStatusService;
        _changeRoomOperationalAvailabilityService = changeRoomOperationalAvailabilityService;
        _addRoomImageService = addRoomImageService;
        _deleteRoomImageService = deleteRoomImageService;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAsync([FromQuery] RoomFilterDto filter)
    {
        var rooms = await _getAllRoomsService.GetAllRoomsAsync(filter);
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
    
    [HttpPost("{roomId:int}/images")]
    public async Task<IActionResult> AddImageAsync(int roomId, AddRoomImageRequestDto request)
    {
        await _addRoomImageService.AddRoomImageAsync(roomId, request);
        return NoContent();
    }
    
    [HttpDelete("{roomId:int}/images/{imageId:int}")]
    public async Task<IActionResult> DeleteImageAsync(int roomId, int imageId)
    {
        await _deleteRoomImageService.DeleteRoomImageAsync(roomId, imageId);
        return NoContent();
    }
}