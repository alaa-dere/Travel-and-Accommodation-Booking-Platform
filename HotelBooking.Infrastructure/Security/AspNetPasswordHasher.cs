using HotelBooking.Application.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace HotelBooking.Infrastructure.Security;

public class AspNetPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> _passwordHasher;
    private readonly object _passwordHasherContext;

    public AspNetPasswordHasher()
    {
        _passwordHasher = new PasswordHasher<object>();
        _passwordHasherContext = new object();
    }

    public string HashPassword(string password)
    {
        var result = _passwordHasher.HashPassword(_passwordHasherContext, password);
        return result;
    }

    public bool VerifyPassword(string hashedPassword, string password)
    {
        var result = _passwordHasher.VerifyHashedPassword(_passwordHasherContext, hashedPassword, password);
        return result != PasswordVerificationResult.Failed;
    }
}