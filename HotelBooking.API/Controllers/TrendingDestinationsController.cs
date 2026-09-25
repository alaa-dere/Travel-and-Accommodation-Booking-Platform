using HotelBooking.Application.TrendingDestinations;
using HotelBooking.Application.TrendingDestinations.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.API.Controllers;

[ApiController]
[Route("api/hotels/trending-destinations")]
[Authorize(Roles = "Customer")]
public class TrendingDestinationsController : ControllerBase
{
    private readonly IGetTrendingDestinationsService _getTrendingDestinationsService;

    public TrendingDestinationsController(
        IGetTrendingDestinationsService getTrendingDestinationsService)
    {
        _getTrendingDestinationsService = getTrendingDestinationsService;
    }

    [HttpGet]
    public async Task<ActionResult<List<TrendingDestinationResponseDto>>> GetTrendingDestinationsAsync()
    {
        var destinations = await _getTrendingDestinationsService.GetTrendingDestinationsAsync();
        return Ok(destinations);
    }
}