using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;

namespace HotelBooking.Application.Bookings;

public class BookingAvailabilityService : IBookingAvailabilityService
{
    private readonly IBookingRepository _bookingRepository;

    public BookingAvailabilityService(IBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository;
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

        var hasConflict = await _bookingRepository.HasConflictingBookingAsync(roomId, checkIn, checkOut, excludedBookingId);
        return !hasConflict;
    }
}