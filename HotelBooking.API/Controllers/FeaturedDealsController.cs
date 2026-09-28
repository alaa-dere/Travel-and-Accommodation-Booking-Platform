using HotelBooking.Application.FeatureDeals;
using HotelBooking.Application.FeatureDeals.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Customer")]
public class FeaturedDealsController : ControllerBase
{
    private readonly IFeaturedDealsService _featuredDealsService;

    public FeaturedDealsController(IFeaturedDealsService featuredDealsService)
    {
        _featuredDealsService = featuredDealsService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FeaturedDealResponseDto>>> GetFeaturedDealsAsync()
    {
        var result = await _featuredDealsService.GetFeaturedDealsAsync();
        return Ok(result);
    }
}
