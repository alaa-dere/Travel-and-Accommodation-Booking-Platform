using HotelBooking.Application.Cart.Dtos;

namespace HotelBooking.Application.Cart;

public interface IGetCartService
{
    Task<IEnumerable<CartItemResponseDto>> GetCartAsync(int userId);
}