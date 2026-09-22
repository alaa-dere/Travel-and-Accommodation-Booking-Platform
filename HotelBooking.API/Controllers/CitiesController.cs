using HotelBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles =  "Admin")]
public class CitiesController : ControllerBase
{
    private readonly IGetAllCitiesService _getAllCitiesService;
    public CitiesController(IGetAllCitiesService getAllCitiesService)
    {
        _getAllCitiesService = getAllCitiesService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync([FromQuery] string? search)
    {
        var cities = await _getAllCitiesService.GetAllCitiesAsync(search);
        return Ok(cities);
    }
}