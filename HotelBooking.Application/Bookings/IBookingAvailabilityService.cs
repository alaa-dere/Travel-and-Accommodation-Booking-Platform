namespace HotelBooking.Application.Bookings;

public interface IBookingAvailabilityService
{
    Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut, int? excludedBookingId = null);
}