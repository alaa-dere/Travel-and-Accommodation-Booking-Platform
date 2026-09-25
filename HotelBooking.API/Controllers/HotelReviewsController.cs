using System.Security.Claims;
using HotelBooking.Application.HotelReviews;
using HotelBooking.Application.HotelReviews.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.API.Controllers;

[ApiController]
[Route("api/hotels")]
[Authorize(Roles = "Customer")]
public class HotelReviewsController : ControllerBase
{
    private readonly IGetHotelReviewsService _getHotelReviewsService;
    private readonly ISubmitHotelReviewService _submitHotelReviewService;
    public HotelReviewsController(IGetHotelReviewsService getHotelReviewsService,  ISubmitHotelReviewService submitHotelReviewService)
    {
        _getHotelReviewsService = getHotelReviewsService;
        _submitHotelReviewService = submitHotelReviewService;
    }

    [HttpGet("{hotelId:int}/reviews")]
    public async Task<IActionResult> GetHotelReviewsAsync(int hotelId)
    {
        var result = await _getHotelReviewsService.GetHotelReviewsAsync(hotelId);
        return Ok(result);
    }
    
    [HttpPost("{hotelId:int}/reviews")]
    public async Task<IActionResult> SubmitReviewAsync(int hotelId, [FromBody] SubmitReviewRequestDto request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }
        await _submitHotelReviewService.SubmitReviewAsync(hotelId, userId, request);
        return NoContent();
    }
}