using HotelBooking.Application.Cart.Dtos;
using HotelBooking.Application.Interfaces;

namespace HotelBooking.Application.Cart;

public class GetCartService : IGetCartService
{
    private readonly ICartRepository _cartRepository;

    public GetCartService(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<IEnumerable<CartItemResponseDto>> GetCartAsync(int userId)
    {
        var items = await _cartRepository.GetByUserIdAsync(userId);

        return items.Select(item => new CartItemResponseDto
        {
            CartItemId = item.CartItemId,
            RoomId = item.RoomId,
            RoomNumber = item.Room!.RoomNumber,
            HotelId = item.Room.HotelId,
            HotelName = item.Room.Hotel!.Name,
            CheckIn = item.CheckIn,
            CheckOut = item.CheckOut,
            Adults = item.Adults,
            Children = item.Children
        });
    }
}