using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Enums;

namespace HotelBooking.Application.Bookings.Cancel;

public class CancelBookingService : ICancelBookingService
{
    private readonly IBookingRepository _bookingRepository;

    public CancelBookingService(IBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository;
    }

    public async Task CancelAsync(int bookingId, int userId)
    {
        if (bookingId <= 0)
        {
            throw new BadRequestException("Invalid booking ID.");
        }

        if (userId <= 0)
        {
            throw new BadRequestException("Invalid user ID.");
        }

        var booking = await _bookingRepository.GetByIdForUserAsync( bookingId, userId);

        if (booking == null)
        {
            throw new NotFoundException("Booking not found.");
        }

        if (booking.BookingStatus == BookingStatus.Cancelled)
        {
            throw new ConflictException("Booking is already cancelled.");
        }

        if (DateTime.UtcNow >= booking.CheckIn)
        {
            throw new ConflictException("Booking cannot be cancelled after the stay has started.");
        }

        booking.Cancel();
        await _bookingRepository.SaveChangesAsync();
    }
}