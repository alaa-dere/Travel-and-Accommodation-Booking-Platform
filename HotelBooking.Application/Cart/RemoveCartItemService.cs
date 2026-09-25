using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;

namespace HotelBooking.Application.Cart;

public class RemoveCartItemService : IRemoveCartItemService
{
    private readonly ICartRepository _cartRepository;

    public RemoveCartItemService(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task RemoveAsync(int cartItemId, int userId)
    {
        var item = await _cartRepository.GetByIdAsync(cartItemId);
        if (item == null || item.UserId != userId)
        {
            throw new NotFoundException("Cart item not found.");
        }
        _cartRepository.Delete(item);
        await _cartRepository.SaveChangesAsync();
    }
}