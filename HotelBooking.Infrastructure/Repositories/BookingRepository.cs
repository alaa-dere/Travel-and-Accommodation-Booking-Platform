using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly HotelBookingDbContext _dbContext;

    public BookingRepository(HotelBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> HasConflictingBookingAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludedBookingId = null)
    {
        return await _dbContext.Bookings.AsNoTracking()
            .AnyAsync(booking => booking.RoomId == roomId && booking.BookingStatus != BookingStatus.Cancelled &&
                                 booking.CheckIn < checkOut && booking.CheckOut > checkIn &&
                                 (!excludedBookingId.HasValue || booking.BookingId != excludedBookingId.Value));
    }
    
    public async Task AddAsync(Booking booking)
    {
        await _dbContext.Bookings.AddAsync(booking);
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
    
    public async Task<Booking?> GetByIdForUserAsync(int bookingId, int userId)
    {
        return await _dbContext.Bookings
            .Include(booking => booking.Invoice)
            .ThenInclude(invoice => invoice!.Bookings)
            .FirstOrDefaultAsync(booking => booking.BookingId == bookingId && booking.UserId == userId);
    }
}