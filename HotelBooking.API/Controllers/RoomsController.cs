using HotelBooking.Application.Common;
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
[Authorize(Roles = "Admin")]
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
        IUpdateRoomService updateRoomService,
        IChangeRoomStatusService changeRoomStatusService,
        IChangeRoomOperationalAvailabilityService changeRoomOperationalAvailabilityService,
        IAddRoomImageService addRoomImageService,
        IDeleteRoomImageService deleteRoomImageService)
    {
        _createRoomService = createRoomService;
        _getAllRoomsService = getAllRoomsService;
        _updateRoomService = updateRoomService;
        _changeRoomStatusService = changeRoomStatusService;
        _changeRoomOperationalAvailabilityService = changeRoomOperationalAvailabilityService;
        _addRoomImageService = addRoomImageService;
        _deleteRoomImageService = deleteRoomImageService;
    }
    
    [HttpGet]
    public async Task<ActionResult<PagedResult<RoomResponseDto>>> GetRoomsAsync([FromQuery] RoomFilterDto filter)
    {
        var rooms = await _getAllRoomsService.GetAllRoomsAsync(filter);
        return Ok(rooms);
    }
    
    [HttpPost]
    public async Task<ActionResult<RoomResponseDto>> CreateRoomAsync(RoomRequestDto request)
    {
        var room = await _createRoomService.CreateRoomAsync(request);
        return Created("api/rooms", room);
    }
    
    [HttpPut("{roomId:int:min(1)}")]
    public async Task<ActionResult<RoomResponseDto>> PutAsync(int roomId, RoomRequestDto request)
    {
        var room = await _updateRoomService.UpdateRoomAsync(roomId, request);
        return Ok(room);
    }
    
    [HttpPatch("{roomId:int:min(1)}/status")]
    public async Task<IActionResult> ChangeStatusAsync(int roomId, ChangeRoomStatusRequest request)
    { 
        await _changeRoomStatusService.ChangeRoomStatusAsync(roomId, request.IsActive!.Value);
        return NoContent();
    }
    
    [HttpPatch("{roomId:int:min(1)}/operational-availability")]
    public async Task<IActionResult> ChangeOperationalAvailabilityAsync(int roomId, ChangeRoomOperationalAvailabilityRequest request)
    {
        await _changeRoomOperationalAvailabilityService.ChangeOperationalAvailabilityAsync(roomId, request.IsOperationallyAvailable!.Value);
        return NoContent();
    }
    
    [HttpPost("{roomId:int:min(1)}/images")]
    public async Task<IActionResult> AddImageAsync(int roomId, AddRoomImageRequestDto request)
    {
        await _addRoomImageService.AddRoomImageAsync(roomId, request);
        return NoContent();
    }
    
    [HttpDelete("{roomId:int:min(1)}/images/{imageId:int:min(1)}")]
    public async Task<IActionResult> DeleteImageAsync(int roomId, int imageId)
    {
        await _deleteRoomImageService.DeleteRoomImageAsync(roomId, imageId);
        return NoContent();
    }
}
