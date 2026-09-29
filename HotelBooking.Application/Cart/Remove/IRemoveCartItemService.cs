namespace HotelBooking.Application.Cart;

public interface IRemoveCartItemService
{
    Task RemoveAsync(int cartItemId, int userId);
}