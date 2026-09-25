using HotelBooking.Application.NearbyAttractions.Create;
using HotelBooking.Application.NearbyAttractions.Dtos;
using HotelBooking.Application.NearbyAttractions.GetByHotel;
using HotelBooking.Application.NearbyAttractions.Remove;
using HotelBooking.Application.NearbyAttractions.Update;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.API.Controllers;

[ApiController]
[Route("api/nearby-attractions")]
[Authorize(Roles = "Admin")]
public class NearbyAttractionsController : ControllerBase
{
    private readonly ICreateNearbyAttractionService _createService;
    private readonly IGetNearbyAttractionsService _getService;
    private readonly IUpdateNearbyAttractionService _updateService;
    private readonly IRemoveNearbyAttractionService _removeService;

    public NearbyAttractionsController(
        ICreateNearbyAttractionService createService,
        IGetNearbyAttractionsService getService,
        IUpdateNearbyAttractionService updateService,
        IRemoveNearbyAttractionService removeService)
    {
        _createService = createService;
        _getService = getService;
        _updateService = updateService;
        _removeService = removeService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateNearbyAttractionRequestDto request)
    {
        await _createService.CreateAsync(request);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpGet("hotel/{hotelId:int}")]
    public async Task<IActionResult> GetByHotel(int hotelId)
    {
        var attractions = await _getService.GetByHotelIdAsync(hotelId);
        return Ok(attractions);
    }

    [HttpPut("{attractionId:int}")]
    public async Task<IActionResult> Update(int attractionId, [FromBody] UpdateNearbyAttractionRequestDto request)
    {
        await _updateService.UpdateAsync(attractionId, request);
        return NoContent();
    }

    [HttpDelete("{attractionId:int}")]
    public async Task<IActionResult> Remove(int attractionId)
    {
        await _removeService.RemoveAsync(attractionId);
        return NoContent();
    }
}