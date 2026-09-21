using HotelBooking.Application.Authentication.Register;

namespace HotelBooking.Application.Interfaces;

public interface IRegisterService
{ 
    Task RegisterAsync(RegisterRequestDto request);
}