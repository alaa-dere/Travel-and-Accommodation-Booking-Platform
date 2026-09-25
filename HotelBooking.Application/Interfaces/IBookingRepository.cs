using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Interfaces;

public interface IBookingRepository
{
    Task<bool> HasConflictingBookingAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludedBookingId = null); Task AddAsync(Booking booking);
    Task SaveChangesAsync();
    Task<Booking?> GetByIdForUserAsync(int bookingId, int userId);
}