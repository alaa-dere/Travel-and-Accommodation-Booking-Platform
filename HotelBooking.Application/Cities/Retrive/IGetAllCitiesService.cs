using HotelBooking.Application.Cities;

namespace HotelBooking.Application.Interfaces;

public interface IGetAllCitiesService
{
    Task<IEnumerable<CityResponseDto>> GetAllCitiesAsync(string? search);
}