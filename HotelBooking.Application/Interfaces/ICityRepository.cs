using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Interfaces;

public interface ICityRepository
{ 
    Task<IEnumerable<City>> GetCitiesAsync(string? search);
    void Add(City city);
    Task SaveChangesAsync();
    Task<City?> GetCityByIdAsync(int id);
    Task<bool> HasHotelsAsync(int id);
    void DeleteCity(City city);
}