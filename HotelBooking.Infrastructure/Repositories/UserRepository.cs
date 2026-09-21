using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly HotelBookingDbContext _dbContext;
    public UserRepository(HotelBookingDbContext  dbContext)
    {
        _dbContext = dbContext ;
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        bool exists = await _dbContext.Users.AnyAsync(u => u.Username == username); 
        return exists;
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        bool exists = await _dbContext.Users.AnyAsync(u => u.Email == email);
        return exists;
    }

    public void Add(User user)
    {
        _dbContext.Users.Add(user);
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        User? user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
        return user;
    }
}