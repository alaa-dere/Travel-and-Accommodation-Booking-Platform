using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.RecentlyVisitedHotels;

public class RecordHotelVisitService : IRecordHotelVisitService
{
    private readonly IRecentlyVisitedHotelRepository _recentlyVisitedHotelRepository;

    public RecordHotelVisitService(IRecentlyVisitedHotelRepository recentlyVisitedHotelRepository)
    {
        _recentlyVisitedHotelRepository = recentlyVisitedHotelRepository;
    }

    public async Task RecordVisitAsync(int userId, int hotelId)
    {
        var existingVisit = await _recentlyVisitedHotelRepository.GetVisitAsync(userId, hotelId);

        if (existingVisit != null)
        {
            existingVisit.VisitedAt = DateTime.UtcNow;
        }
        else
        {
            var visit = new RecentlyVisitedHotel
            {
                UserId = userId,
                HotelId = hotelId,
                VisitedAt = DateTime.UtcNow
            };

            await _recentlyVisitedHotelRepository.AddAsync(visit);
        }

        await _recentlyVisitedHotelRepository.SaveChangesAsync();
    }
}