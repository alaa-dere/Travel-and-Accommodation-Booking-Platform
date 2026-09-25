using HotelBooking.Application.Bookings.Dtos;

namespace HotelBooking.Application.Bookings.Modify;

public interface IModifyBookingService
{
    Task ModifyAsync(int bookingId, int userId, ModifyBookingRequestDto request);
}