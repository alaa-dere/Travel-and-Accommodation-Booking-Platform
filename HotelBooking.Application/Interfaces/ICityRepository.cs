using HotelBooking.Application.Cities;
using HotelBooking.Application.Common;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Interfaces;

public interface ICityRepository
{ 
    Task<PagedResult<CityResponseDto>> GetCitiesAsync(CityListRequestDto request);
    void Add(City city);
    Task SaveChangesAsync();
    Task<City?> GetCityByIdAsync(int id);
    Task<bool> HasHotelsAsync(int id);
    Task<int> GetHotelsCountAsync(int cityId);
    Task<bool> ExistsAsync(string name, string country, string postOffice, int? excludedCityId = null);
    void DeleteCity(City city);
}
