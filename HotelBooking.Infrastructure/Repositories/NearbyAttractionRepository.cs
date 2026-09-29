using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Exceptions;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace HotelBooking.Infrastructure.Repositories;

public class NearbyAttractionRepository : INearbyAttractionRepository
{
    private readonly HotelBookingDbContext _dbContext;

    public NearbyAttractionRepository(HotelBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(NearbyAttraction attraction)
    {
        await _dbContext.NearbyAttractions.AddAsync(attraction);
    }

    public async Task<IEnumerable<NearbyAttraction>> GetByHotelIdAsync(int hotelId)
    {
        return await _dbContext.NearbyAttractions
            .AsNoTracking()
            .Where(attraction => attraction.HotelId == hotelId)
            .OrderBy(attraction => attraction.Name)
            .ToListAsync();
    }

    public async Task<NearbyAttraction?> GetByIdAsync(int attractionId)
    {
        return await _dbContext.NearbyAttractions.FirstOrDefaultAsync(attraction => attraction.NearbyAttractionId == attractionId);
    }

    public Task<bool> ExistsAsync(int hotelId, string name, int? excludedAttractionId = null)
    {
        return _dbContext.NearbyAttractions.AsNoTracking().AnyAsync(attraction =>
            attraction.HotelId == hotelId &&
            attraction.Name == name &&
            (!excludedAttractionId.HasValue ||
             attraction.NearbyAttractionId != excludedAttractionId.Value));
    }

    public void Remove(NearbyAttraction attraction)
    {
        _dbContext.NearbyAttractions.Remove(attraction);
    }

    public async Task SaveChangesAsync()
    {
        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new ConflictException("An attraction with the same name already exists for this hotel.");
        }
    }
}
