using HotelBooking.Application.HotelReviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.API.Controllers;

[ApiController]
[Route("api/hotels")]
[Authorize(Roles = "Customer")]
public class HotelReviewsController : ControllerBase
{
    private readonly IGetHotelReviewsService _getHotelReviewsService;

    public HotelReviewsController(IGetHotelReviewsService getHotelReviewsService)
    {
        _getHotelReviewsService = getHotelReviewsService;
    }

    [HttpGet("{hotelId}/reviews")]
    public async Task<IActionResult> GetHotelReviewsAsync(int hotelId)
    {
        var result = await _getHotelReviewsService.GetHotelReviewsAsync(hotelId);
        return Ok(result);
    }
}