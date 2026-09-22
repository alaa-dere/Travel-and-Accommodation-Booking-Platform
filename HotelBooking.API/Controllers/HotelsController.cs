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
[Authorize(Roles =  "Admin")]
public class HotelsController : ControllerBase
{
    private readonly ICreateHotelService _createHotelService;
    private readonly IGetAllHotelsService _getAllHotelsService;
    private readonly IUpdateHotelService _updateHotelService;
    private readonly IChangeHotelStatusService _changeHotelStatusService;
    public HotelsController(ICreateHotelService createHotelService, IGetAllHotelsService getAllHotelsService, IUpdateHotelService updateHotelService, IChangeHotelStatusService changeHotelStatusService)
    {
        _createHotelService = createHotelService;
        _getAllHotelsService = getAllHotelsService;
        _updateHotelService = updateHotelService;
        _changeHotelStatusService = changeHotelStatusService;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAsync([FromQuery] string? search)
    {
        var hotels = await _getAllHotelsService.GetAllHotelsAsync(search);
        return Ok(hotels);
    }

    [HttpPost]
    public async Task<IActionResult> CreateHotelAsync(HotelRequestDto request)
    {
        var hotel = await _createHotelService.CreateHotelAsync(request);
        return Created("api/hotels", hotel);
    }
    
    [HttpPut("{hotelId}")]
    public async Task<IActionResult> PutAsync(int hotelId, HotelRequestDto request)
    {
        var hotel = await _updateHotelService.UpdateHotelAsync(hotelId, request);
        return Ok(hotel);
    }
    
   [HttpPatch("{hotelId}/status")]
    public async Task<IActionResult> ChangeStatusAsync(int hotelId, ChangeHotelStatusRequest request)
    { 
        await _changeHotelStatusService.ChangeHotelStatusAsync(hotelId, request.IsActive);
        return NoContent();
    }
}