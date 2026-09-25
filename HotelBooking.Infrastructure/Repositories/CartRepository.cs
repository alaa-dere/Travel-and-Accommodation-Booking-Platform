using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Repositories;

public class CartRepository : ICartRepository
{
    private readonly HotelBookingDbContext _dbContext;

    public CartRepository(HotelBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(CartItem cartItem)
    {
        await _dbContext.CartItems.AddAsync(cartItem);
    }

    public async Task<List<CartItem>> GetByUserIdAsync(int userId)
    {
        return await _dbContext.CartItems
            .AsNoTracking()
            .Include(cartItem => cartItem.Room)
            .ThenInclude(room => room!.Hotel)
            .Where(cartItem => cartItem.UserId == userId)
            .OrderBy(cartItem => cartItem.CreatedAt)
            .ToListAsync();
    }

    public async Task<CartItem?> GetByIdAsync(int cartItemId)
    {
        return await _dbContext.CartItems
            .FirstOrDefaultAsync(cartItem => cartItem.CartItemId == cartItemId);
    }

    public void Delete(CartItem cartItem)
    {
        _dbContext.CartItems.Remove(cartItem);
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
    
    public void DeleteRange(IEnumerable<CartItem> cartItems)
    {
        _dbContext.CartItems.RemoveRange(cartItems);
    }
}