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

    public async Task<bool> HasConflictingBookingAsync(int roomId, DateTime checkIn, DateTime checkOut)
    {
        return await _dbContext.Bookings.AsNoTracking()
            .AnyAsync(booking => booking.RoomId == roomId && booking.BookingStatus != BookingStatus.Cancelled &&
                                 booking.CheckIn < checkOut && booking.CheckOut > checkIn);
    }
}