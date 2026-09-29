namespace HotelBooking.Application.RecentlyVisitedHotels;

public interface IRecordHotelVisitService
{
    Task RecordVisitAsync(int userId, int hotelId);
}