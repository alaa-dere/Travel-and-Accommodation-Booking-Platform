using HotelBooking.Application.Cart.Dtos;

namespace HotelBooking.Application.Cart.Retrive;

public interface IGetCartService
{
    Task<IEnumerable<CartItemResponseDto>> GetCartAsync(int userId);
}