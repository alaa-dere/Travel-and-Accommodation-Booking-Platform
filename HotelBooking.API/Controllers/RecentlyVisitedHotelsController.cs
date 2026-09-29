using System.Security.Claims;
using HotelBooking.Application.RecentlyVisitedHotels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.API.Controllers;

[ApiController]
[Route("api/hotels/recently-visited")]
[Authorize(Roles = "Customer")]
public class RecentlyVisitedHotelsController : ControllerBase
{
    private readonly IGetRecentlyVisitedHotelsService _getRecentlyVisitedHotelsService;

    public RecentlyVisitedHotelsController(IGetRecentlyVisitedHotelsService getRecentlyVisitedHotelsService)
    {
        _getRecentlyVisitedHotelsService = getRecentlyVisitedHotelsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecentlyVisitedHotelsAsync()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }
        var hotels = await _getRecentlyVisitedHotelsService.GetRecentlyVisitedHotelsAsync(userId);
        return Ok(hotels);
    }
}