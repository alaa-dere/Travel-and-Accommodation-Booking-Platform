using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Interfaces;

public interface IUserRepository
{
     Task<bool> UsernameExistsAsync(string username);
     Task<bool> EmailExistsAsync(string email);
     void Add(User user);
     Task SaveChangesAsync();
     Task<User?> GetByUsernameAsync(string username);
}