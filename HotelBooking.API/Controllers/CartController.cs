using System.Security.Claims;
using HotelBooking.Application.Cart;
using HotelBooking.Application.Cart.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.API.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize(Roles = "Customer")]
public class CartController : ControllerBase
{
    private readonly IAddCartItemService _addCartItemService;
    private readonly IGetCartService _getCartService;
    private readonly IRemoveCartItemService _removeCartItemService;

    public CartController(
        IAddCartItemService addCartItemService,
        IGetCartService getCartService,
        IRemoveCartItemService removeCartItemService)
    {
        _addCartItemService = addCartItemService;
        _getCartService = getCartService;
        _removeCartItemService = removeCartItemService;
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItemAsync(AddCartItemRequestDto request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }
        await _addCartItemService.AddCartItemAsync(userId, request);
        return NoContent();
    }

    [HttpGet]
    public async Task<IActionResult> GetCartAsync()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }
        var cart = await _getCartService.GetCartAsync(userId);
        return Ok(cart);
    }

    [HttpDelete("items/{cartItemId:int}")]
    public async Task<IActionResult> RemoveItemAsync(int cartItemId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }
        await _removeCartItemService.RemoveAsync(cartItemId, userId);
        return NoContent();
    }
}