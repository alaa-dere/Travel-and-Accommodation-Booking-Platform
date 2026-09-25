namespace HotelBooking.Application.Bookings.Cancel;

public interface ICancelBookingService
{
    Task CancelAsync(int bookingId, int userId);
}