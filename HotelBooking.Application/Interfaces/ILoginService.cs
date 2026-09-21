using HotelBooking.Application.Authentication.Login;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Interfaces;

public interface ILoginService
{
    Task LoginAsync(LoginRequestDto request);
}