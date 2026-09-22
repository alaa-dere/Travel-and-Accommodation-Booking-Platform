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
    private readonly IUpdateCityService _updateCityService;

    public CitiesController(IGetAllCitiesService getAllCitiesService, ICreateCityService  createCityService, IUpdateCityService  updateCityService)
    {
        _getAllCitiesService = getAllCitiesService;
        _createCityService = createCityService;
        _updateCityService = updateCityService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync([FromQuery] string? search)
    {
        var cities = await _getAllCitiesService.GetAllCitiesAsync(search);
        return Ok(cities);
    }

    [HttpPost]
    public async Task<IActionResult> PostAsync(CityRequestDto request)
    {
        var city = await _createCityService.CreateCityAsync(request);
        return Created("api/cities", city);
    }

    [HttpPut("{cityId}")]
    public async Task<IActionResult> PutAsync(int cityId, CityRequestDto request)
    {
        var city = await _updateCityService.UpdateCityAsync(cityId, request);
        return Ok(city);
    }
}