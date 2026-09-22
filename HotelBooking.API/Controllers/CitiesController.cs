using HotelBooking.Application.Cities;
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
    private readonly ICreateCityService _createCityService;

    public CitiesController(IGetAllCitiesService getAllCitiesService, ICreateCityService  createCityService)
    {
        _getAllCitiesService = getAllCitiesService;
        _createCityService = createCityService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync([FromQuery] string? search)
    {
        var cities = await _getAllCitiesService.GetAllCitiesAsync(search);
        return Ok(cities);
    }

    [HttpPost]
    public async Task<IActionResult> PostAsync(CreateCityRequestDto request)
    {
        var city = await _createCityService.CreateCityAsync(request);
        return Created("api/cities", city);
    }
}