using HotelBooking.Application.Cities;
using HotelBooking.Application.Cities.Delete;
using HotelBooking.Application.Common;
using HotelBooking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class CitiesController : ControllerBase
{
    private readonly IGetAllCitiesService _getAllCitiesService;
    private readonly ICreateCityService _createCityService;
    private readonly IUpdateCityService _updateCityService;
    private readonly IDeleteCityService _deleteCityService;

    public CitiesController(
        IGetAllCitiesService getAllCitiesService, ICreateCityService createCityService, IUpdateCityService updateCityService, IDeleteCityService deleteCityService)
    {
        _getAllCitiesService = getAllCitiesService;
        _createCityService = createCityService;
        _updateCityService = updateCityService;
        _deleteCityService = deleteCityService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<CityResponseDto>>> GetCitiesAsync([FromQuery] CityListRequestDto request)
    {
        var cities = await _getAllCitiesService.GetAllCitiesAsync(request);
        return Ok(cities);
    }

    [HttpPost]
    public async Task<ActionResult<CityResponseDto>> CreateCityAsync(CityRequestDto request)
    {
        var city = await _createCityService.CreateCityAsync(request);
        return StatusCode(StatusCodes.Status201Created, city);
    }

    [HttpPut("{cityId:int}")]
    public async Task<ActionResult<CityResponseDto>> UpdateCityAsync(int cityId, CityRequestDto request)
    {
        var city = await _updateCityService.UpdateCityAsync(cityId, request);
        return Ok(city);
    }
    
    [HttpDelete("{cityId:int}")]
    public async Task<IActionResult> DeleteCityAsync(int cityId)
    { 
        await _deleteCityService.DeleteCityAsync(cityId);
        return NoContent();
    }
}
