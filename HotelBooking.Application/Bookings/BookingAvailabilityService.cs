using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;

namespace HotelBooking.Application.Bookings;

public class BookingAvailabilityService : IBookingAvailabilityService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly TimeProvider _timeProvider;

    public BookingAvailabilityService(IBookingRepository bookingRepository, TimeProvider timeProvider)
    {
        _bookingRepository = bookingRepository;
        _timeProvider = timeProvider;
    }

    public async Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludedBookingId = null)
    {
        if (roomId <= 0)
        {
            throw new BadRequestException("Invalid room ID.");
        }

        if (checkOut <= checkIn)
        {
            throw new BadRequestException("Check-out date must be after check-in date.");
        }

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var hasConflict = await _bookingRepository.HasConflictingBookingAsync(roomId, checkIn, checkOut, utcNow, excludedBookingId);
        return !hasConflict;
    }
}
