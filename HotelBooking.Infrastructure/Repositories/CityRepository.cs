using HotelBooking.Application.Cities;
using HotelBooking.Application.Common;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Repositories;

public class CityRepository : ICityRepository
{
    private readonly HotelBookingDbContext _dbContext;
    public CityRepository(HotelBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<CityResponseDto>> GetCitiesAsync(CityListRequestDto request)
    {
        var query = _dbContext.Cities.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var normalizedSearch = request.Search.Trim();
            query = query.Where(city => city.Name.Contains(normalizedSearch) || city.Country.Contains(normalizedSearch));
        }

        const int pageSize = 10;
        var items = await query
            .OrderBy(city => city.CityId)
            .Skip((request.PageNumber - 1) * pageSize)
            .Take(pageSize + 1)
            .Select(city => new CityResponseDto
            {
                CityId = city.CityId,
                Name = city.Name,
                Country = city.Country,
                PostOffice = city.PostOffice,
                ThumbnailUrl = city.ThumbnailUrl,
                HotelsCount = city.Hotels.Count,
                CreatedAt = city.CreatedAt,
                UpdatedAt = city.UpdatedAt
            })
            .ToListAsync();

        return new PagedResult<CityResponseDto>
        {
            Items = items.Take(pageSize),
            PageNumber = request.PageNumber,
            HasNextPage = items.Count > pageSize
        };
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
        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new ConflictException("A city with the same name, country, and post office already exists.");
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException { Number: 547 })
        {
            throw new ConflictException("City cannot be deleted because it has associated hotels.");
        }
    }
    
    public Task<bool> HasHotelsAsync(int cityId)
    {
        return _dbContext.Hotels.AnyAsync(h => h.CityId == cityId);
    }

    public Task<int> GetHotelsCountAsync(int cityId)
    {
        return _dbContext.Hotels.CountAsync(hotel => hotel.CityId == cityId);
    }

    public Task<bool> ExistsAsync(string name, string country, string postOffice, int? excludedCityId = null)
    {
        return _dbContext.Cities.AsNoTracking().AnyAsync(city =>
            city.Name == name && city.Country == country && city.PostOffice == postOffice &&
            (!excludedCityId.HasValue || city.CityId != excludedCityId.Value));
    }
    
    public void DeleteCity(City city)
    {
        _dbContext.Cities.Remove(city);
    }
}
