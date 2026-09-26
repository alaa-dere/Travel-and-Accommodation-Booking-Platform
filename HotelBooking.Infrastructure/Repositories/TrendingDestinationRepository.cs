using HotelBooking.Application.Interfaces;
using HotelBooking.Application.TrendingDestinations.Dtos;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Repositories;

public class TrendingDestinationRepository : ITrendingDestinationRepository
{
    private readonly HotelBookingDbContext _dbContext;

    public TrendingDestinationRepository(HotelBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<TrendingDestinationResponseDto>> GetTrendingDestinationsAsync(DateTime fromDate)
    {
        return await _dbContext.Bookings.AsNoTracking()
            .Where(booking =>
                booking.CreatedAt >= fromDate &&
                booking.BookingStatus != BookingStatus.Cancelled &&
                booking.Room!.IsActive &&
                booking.Room.IsOperationallyAvailable &&
                booking.Room.Hotel!.IsActive)
            .GroupBy(booking => new
            {
                booking.Room!.Hotel!.CityId,
                booking.Room.Hotel.City.Name,
                booking.Room.Hotel.City.Country
            })
            .Select(group => new TrendingDestinationResponseDto
            {
                CityId = group.Key.CityId,
                Name = group.Key.Name,
                Country = group.Key.Country,
                BookingCount = group.Count()
            })
            .OrderByDescending(destination => destination.BookingCount)
            .ThenBy(destination => destination.CityId)
            .Take(5)
            .ToListAsync();
    }
}
