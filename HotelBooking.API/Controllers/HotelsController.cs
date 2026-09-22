using HotelBooking.Application.Hotels.Create;
using HotelBooking.Application.Hotels.Dtos;
using HotelBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles =  "Admin")]
public class HotelsController : ControllerBase
{
    private readonly ICreateHotelService _createHotelService;
    public HotelsController(ICreateHotelService  createHotelService)
    {
        _createHotelService = createHotelService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateHotelAsync(HotelRequestDto request)
    {
        var hotel = await _createHotelService.CreateHotelAsync(request);
        return Created("api/hotels", hotel);
    }
}