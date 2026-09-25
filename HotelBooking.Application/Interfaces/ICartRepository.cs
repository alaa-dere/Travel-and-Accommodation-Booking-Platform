using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Interfaces;

public interface ICartRepository
{
    Task AddAsync(CartItem cartItem);
    Task<List<CartItem>> GetByUserIdAsync(int userId);
    Task<CartItem?> GetByIdAsync(int cartItemId);
    void Delete(CartItem cartItem);
    Task SaveChangesAsync();
    void DeleteRange(IEnumerable<CartItem> cartItems);
}