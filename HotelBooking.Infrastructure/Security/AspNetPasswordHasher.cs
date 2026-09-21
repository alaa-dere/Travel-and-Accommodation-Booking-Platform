using HotelBooking.Application.Interfaces;
using Microsoft.AspNetCore.Identity;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Infrastructure.Security;

public class AspNetPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _passwordHasher;

    public AspNetPasswordHasher()
    {
        _passwordHasher = new PasswordHasher<User>();
    }
    
    public string HashPassword(string password)
    {
        var result = _passwordHasher.HashPassword(null, password);
        return result;
    }

    public bool VerifyPassword(string hashedPassword, string password)
    {
        var result = _passwordHasher.VerifyHashedPassword(null, hashedPassword, password);
        return result != PasswordVerificationResult.Failed;
    }
}