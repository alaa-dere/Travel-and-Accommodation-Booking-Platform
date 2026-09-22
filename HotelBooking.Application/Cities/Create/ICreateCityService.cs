using HotelBooking.Application.Cities;

namespace HotelBooking.Application.Interfaces;

public interface ICreateCityService
{
    Task<CityResponseDto> CreateCityAsync(CityRequestDto request);
}