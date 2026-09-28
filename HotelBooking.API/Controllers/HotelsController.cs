using HotelBooking.Application.Common;
using HotelBooking.Application.Hotels.Create;
using HotelBooking.Application.Hotels.Delete;
using HotelBooking.Application.Hotels.Dtos;
using HotelBooking.Application.Hotels.Retrive;
using HotelBooking.Application.Hotels.Update;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class HotelsController : ControllerBase
{
    private readonly ICreateHotelService _createHotelService;
    private readonly IGetAllHotelsService _getAllHotelsService;
    private readonly IUpdateHotelService _updateHotelService;
    private readonly IChangeHotelStatusService _changeHotelStatusService;
    public HotelsController(
        ICreateHotelService createHotelService,
        IGetAllHotelsService getAllHotelsService,
        IUpdateHotelService updateHotelService,
        IChangeHotelStatusService changeHotelStatusService)
    {
        _createHotelService = createHotelService;
        _getAllHotelsService = getAllHotelsService;
        _updateHotelService = updateHotelService;
        _changeHotelStatusService = changeHotelStatusService;
    }
    
    [HttpGet]
    public async Task<ActionResult<PagedResult<HotelResponseDto>>> GetHotelsAsync([FromQuery] HotelListRequestDto request)
    {
        var hotels = await _getAllHotelsService.GetAllHotelsAsync(request);
        return Ok(hotels);
    }

    [HttpPost]
    public async Task<ActionResult<HotelResponseDto>> CreateHotelAsync(HotelRequestDto request)
    {
        var hotel = await _createHotelService.CreateHotelAsync(request);
        return StatusCode(StatusCodes.Status201Created, hotel);
    }
    
    [HttpPut("{hotelId:int:min(1)}")]
    public async Task<ActionResult<HotelResponseDto>> UpdateHotelAsync(int hotelId, HotelRequestDto request)
    {
        var hotel = await _updateHotelService.UpdateHotelAsync(hotelId, request);
        return Ok(hotel);
    }
    
    [HttpPatch("{hotelId:int:min(1)}/status")]
    public async Task<IActionResult> ChangeStatusAsync(int hotelId, ChangeHotelStatusRequest request)
    { 
        await _changeHotelStatusService.ChangeHotelStatusAsync(hotelId, request.IsActive!.Value);
        return NoContent();
    }
}
