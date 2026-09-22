using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Interfaces;

public interface ICityRepository
{ 
    Task<IEnumerable<City>> GetCitiesAsync(string? search);
}