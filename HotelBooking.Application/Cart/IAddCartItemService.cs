using HotelBooking.Application.Cart.Dtos;

namespace HotelBooking.Application.Cart;

public interface IAddCartItemService
{
    Task AddCartItemAsync(int userId, AddCartItemRequestDto request);
}