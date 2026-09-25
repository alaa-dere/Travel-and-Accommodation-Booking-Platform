using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Interfaces;

public interface IBookingRepository
{
    Task<bool> HasConflictingBookingAsync(int roomId, DateTime checkIn, DateTime checkOut);
    Task AddAsync(Booking booking);
    Task SaveChangesAsync();
}