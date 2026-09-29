using HotelBooking.Application.HotelLocations.Dtos;
using HotelBooking.Application.Interfaces;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Repositories;

public class HotelLocationRepository : IHotelLocationRepository
{
    private readonly HotelBookingDbContext _dbContext;
    public  HotelLocationRepository(HotelBookingDbContext dbContext)
        {
            _dbContext = dbContext;
        }

    public Task<HotelLocationResponseDto?> GetHotelLocationAsync(int hotelId)
    {
        return _dbContext.Hotels.AsNoTracking()
            .Where(hotel => hotel.HotelId == hotelId && hotel.IsActive)
            .Select(hotel => new HotelLocationResponseDto
            {
                Latitude = hotel.Latitude,
                Longitude = hotel.Longitude,
                Attractions = hotel.NearbyAttractions
                    .Select(attraction => new NearbyAttractionResponseDto
                    {
                        Name = attraction.Name,
                        Description = attraction.Description,
                        Latitude = attraction.Latitude,
                        Longitude = attraction.Longitude
                    }).ToList()
            })
            .FirstOrDefaultAsync();
    }
}