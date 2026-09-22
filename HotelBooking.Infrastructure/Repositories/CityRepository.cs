using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Repositories;

public class CityRepository : ICityRepository
{
    private readonly HotelBookingDbContext _dbContext;
    public CityRepository(HotelBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<City>> GetCitiesAsync(string? search)
    {
        var searchQuery = _dbContext.Cities.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            searchQuery = searchQuery.Where(c => c.Name.Contains(search) || c.Country.Contains(search));
        }
        return await searchQuery.ToListAsync();
    }

    public void Add(City city)
    {
        _dbContext.Cities.Add(city);
    }

    public Task<City?> GetCityByIdAsync(int id)
    {
        var city = _dbContext.Cities.FirstOrDefaultAsync(c => c.CityId == id);
        return city;
    }
    
    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}