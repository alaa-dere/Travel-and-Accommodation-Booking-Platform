using HotelBooking.Application.Bookings.Dtos;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Bookings.Modify;

public class ModifyBookingService : IModifyBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly TimeProvider _timeProvider;

    public ModifyBookingService(IBookingRepository bookingRepository, IRoomRepository roomRepository, TimeProvider timeProvider)
    {
        _bookingRepository = bookingRepository;
        _roomRepository = roomRepository;
        _timeProvider = timeProvider;
    }

    public async Task ModifyAsync(int bookingId, int userId, ModifyBookingRequestDto request)
    {
        if (bookingId <= 0 || userId <= 0)
        {
            throw new BadRequestException("Invalid booking or user ID.");
        }

        var booking = await _bookingRepository.GetByIdForUserAsync(bookingId, userId);
        if (booking == null)
        {
            throw new NotFoundException("Booking not found.");
        }

        EnsureBookingCanBeModified(booking);

        var room = await _roomRepository.GetRoomByIdAsync(booking.RoomId);
        if (room == null)
        {
            throw new InvalidOperationException("The room associated with the booking was not found.");
        }

        if (request.Adults > room.AdultsCapacity || request.Children > room.ChildCapacity)
        {
            throw new BadRequestException("The number of guests exceeds the room capacity.");
        }

        booking.UpdateGuestDetails(request.Adults, request.Children, request.SpecialRequests, _timeProvider.GetUtcNow().UtcDateTime);

        await _bookingRepository.SaveChangesAsync();
    }

    private void EnsureBookingCanBeModified(Booking booking)
    {
        if (booking.BookingStatus == BookingStatus.Cancelled)
        {
            throw new ConflictException("Cancelled bookings cannot be modified.");
        }

        if (_timeProvider.GetUtcNow().UtcDateTime >= booking.CheckIn)
        {
            throw new ConflictException("Booking cannot be modified after the stay has started.");
        }
    }
}
