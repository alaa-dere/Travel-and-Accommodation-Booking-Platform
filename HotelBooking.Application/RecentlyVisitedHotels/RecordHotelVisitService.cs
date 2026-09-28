using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.RecentlyVisitedHotels;

public class RecordHotelVisitService : IRecordHotelVisitService
{
    private readonly IRecentlyVisitedHotelRepository _recentlyVisitedHotelRepository;
    private readonly TimeProvider _timeProvider;

    public RecordHotelVisitService(IRecentlyVisitedHotelRepository recentlyVisitedHotelRepository, TimeProvider timeProvider)
    {
        _recentlyVisitedHotelRepository = recentlyVisitedHotelRepository;
        _timeProvider = timeProvider;
    }

    public async Task RecordVisitAsync(int userId, int hotelId)
    {
        var existingVisit = await _recentlyVisitedHotelRepository.GetVisitAsync(userId, hotelId);
        var visitedAt = _timeProvider.GetUtcNow().UtcDateTime;

        if (existingVisit != null)
        {
            existingVisit.VisitedAt = visitedAt;
        }
        else
        {
            var visit = new RecentlyVisitedHotel
            {
                UserId = userId,
                HotelId = hotelId,
                VisitedAt = visitedAt
            };

            await _recentlyVisitedHotelRepository.AddAsync(visit);
        }

        await _recentlyVisitedHotelRepository.SaveChangesAsync();
    }
}
