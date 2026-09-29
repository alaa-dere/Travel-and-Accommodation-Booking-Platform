namespace HotelBooking.Application.Hotels.Delete;

public interface IChangeHotelStatusService
{
    Task ChangeHotelStatusAsync(int hotelId, bool isActive);
}