using HotelBooking.Application.RecentlyVisitedHotels.Dtos;

namespace HotelBooking.Application.RecentlyVisitedHotels;

public interface IGetRecentlyVisitedHotelsService
{
    Task<List<RecentlyVisitedHotelResponseDto>> GetRecentlyVisitedHotelsAsync(int userId);
}