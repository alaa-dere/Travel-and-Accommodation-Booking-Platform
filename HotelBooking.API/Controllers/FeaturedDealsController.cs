using HotelBooking.Application.FeatureDeals;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles =  "Customer")]
public class FeaturedDealsController : ControllerBase
{
    private readonly IFeaturedDealsService _featuredDeals;
    public FeaturedDealsController(IFeaturedDealsService  featuredDealsService)
    {
        _featuredDeals = featuredDealsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync()
    {
        var result = await _featuredDeals.GetFeaturedDealsAsync();
        return Ok(result);
    }
}