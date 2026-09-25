using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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

    public void Remove(NearbyAttraction attraction)
    {
        _dbContext.NearbyAttractions.Remove(attraction);
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}