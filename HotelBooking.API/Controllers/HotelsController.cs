using HotelBooking.Application.Hotels.Create;
using HotelBooking.Application.Hotels.Dtos;
using HotelBooking.Application.Hotels.Retrive;
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
    public HotelsController(ICreateHotelService  createHotelService, IGetAllHotelsService getAllHotelsService)
    {
        _createHotelService = createHotelService;
        _getAllHotelsService = getAllHotelsService;
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
}