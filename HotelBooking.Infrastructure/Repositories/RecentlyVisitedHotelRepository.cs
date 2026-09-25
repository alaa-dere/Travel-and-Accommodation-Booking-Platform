using HotelBooking.Application.Interfaces;
using HotelBooking.Application.RecentlyVisitedHotels.Dtos;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Repositories;

public class RecentlyVisitedHotelRepository : IRecentlyVisitedHotelRepository
{
    private readonly HotelBookingDbContext _dbContext;

    public RecentlyVisitedHotelRepository(HotelBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<RecentlyVisitedHotel?> GetVisitAsync(int userId, int hotelId)
    {
        return _dbContext.RecentlyVisitedHotels.FirstOrDefaultAsync(visit => visit.UserId == userId && visit.HotelId == hotelId);
    }

    public async Task AddAsync(RecentlyVisitedHotel visit)
    {
        await _dbContext.RecentlyVisitedHotels.AddAsync(visit);
    }

    public Task SaveChangesAsync()
    {
        return _dbContext.SaveChangesAsync();
    }
    
    public async Task<List<RecentlyVisitedHotelResponseDto>> GetRecentlyVisitedHotelsAsync(int userId)
    {
        return await _dbContext.RecentlyVisitedHotels.AsNoTracking()
            .Where(visit => visit.UserId == userId && visit.Hotel != null && visit.Hotel.IsActive)
            .OrderByDescending(visit => visit.VisitedAt)
            .Take(5)
            .Select(visit => new RecentlyVisitedHotelResponseDto
            {
                HotelId = visit.HotelId,
                Name = visit.Hotel!.Name,
                City = visit.Hotel.City.Name,
                
                Rating = visit.Hotel.Rooms
                    .SelectMany(room => room.Bookings)
                    .Where(booking => booking.BookingStatus == BookingStatus.Completed && booking.Review != null)
                    .Average(booking => (double?)booking.Review!.Rating),

                StartingPricePerNight = visit.Hotel.Rooms
                    .Where(room => room.IsActive && room.IsOperationallyAvailable)
                    .Min(room => (decimal?)room.PricePerNight)
            }).ToListAsync();
    }
}