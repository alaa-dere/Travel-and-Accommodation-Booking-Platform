using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Interfaces;

public interface IBookingRepository
{
    Task<bool> HasConflictingBookingAsync(int roomId, DateTime checkIn, DateTime checkOut, DateTime utcNow, int? excludedBookingId = null);
    Task AddAsync(Booking booking);
    Task<List<Booking>> GetExpiredPendingBookingsAsync(DateTime utcNow, int batchSize);
    Task SaveChangesAsync();
    Task<Booking?> GetByIdForUserAsync(int bookingId, int userId);
    Task<Booking?> GetByProviderRefundIdAsync(string providerRefundId);
}
