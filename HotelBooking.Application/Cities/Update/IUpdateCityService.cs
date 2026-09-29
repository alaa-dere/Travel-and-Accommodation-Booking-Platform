using HotelBooking.Application.Cities;

namespace HotelBooking.Application.Interfaces;

public interface IUpdateCityService
{
    Task<CityResponseDto> UpdateCityAsync(int id, CityRequestDto request);
}