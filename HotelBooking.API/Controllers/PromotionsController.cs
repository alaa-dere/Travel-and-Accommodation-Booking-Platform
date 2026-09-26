using HotelBooking.Application.Promotions.Create;
using HotelBooking.Application.Promotions.Status;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.API.Controllers;

[ApiController]
[Route("api/promotions")]
[Authorize(Roles = "Admin")]
public class PromotionsController : ControllerBase
{
    private readonly ICreatePromotionService _createPromotionService;
    private readonly IChangePromotionStatusService _changePromotionStatusService;

    public PromotionsController(ICreatePromotionService createPromotionService, IChangePromotionStatusService changePromotionStatusService)
    {
        _createPromotionService = createPromotionService;
        _changePromotionStatusService = changePromotionStatusService;
    }

    [HttpPost]
    public async Task<IActionResult> CreatePromotion([FromBody] CreatePromotionRequestDto request)
    {
        await _createPromotionService.CreateAsync(request);
        return StatusCode(StatusCodes.Status201Created);
    }
    
    [HttpPatch("{promotionId:int}/status")]
    public async Task<IActionResult> ChangePromotionStatus(int promotionId, [FromQuery] bool? isActive)
    {
        if (!isActive.HasValue)
        {
            return BadRequest(new { message = "The isActive query parameter is required." });
        }

        await _changePromotionStatusService.ChangeStatusAsync(promotionId, isActive.Value);
        return NoContent();
    }
}
