namespace HotelBooking.Application.Bookings.Expiration;

public interface IExpirePendingBookingsService
{
    Task<int> ExpireAsync();
}
