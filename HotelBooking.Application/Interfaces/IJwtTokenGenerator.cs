using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}