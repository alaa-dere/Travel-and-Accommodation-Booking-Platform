using HotelBooking.Application.Interfaces;
using HotelBooking.Application.RecentlyVisitedHotels.Dtos;

namespace HotelBooking.Application.RecentlyVisitedHotels;

public class GetRecentlyVisitedHotelsService : IGetRecentlyVisitedHotelsService
{
    private readonly IRecentlyVisitedHotelRepository _repository;

    public GetRecentlyVisitedHotelsService(IRecentlyVisitedHotelRepository repository)
    {
        _repository = repository;
    }

    public Task<List<RecentlyVisitedHotelResponseDto>> GetRecentlyVisitedHotelsAsync(int userId)
    {
        return _repository.GetRecentlyVisitedHotelsAsync(userId);
    }
}