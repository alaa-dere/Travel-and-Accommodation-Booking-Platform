using HotelBooking.Application.Interfaces;
using HotelBooking.Application.TrendingDestinations.Dtos;

namespace HotelBooking.Application.TrendingDestinations;

public class GetTrendingDestinationsService : IGetTrendingDestinationsService
{
    private readonly ITrendingDestinationRepository _trendingDestinationRepository;
    private readonly TimeProvider _timeProvider;

    public GetTrendingDestinationsService(ITrendingDestinationRepository trendingDestinationRepository, TimeProvider timeProvider)
    {
        _trendingDestinationRepository = trendingDestinationRepository;
        _timeProvider = timeProvider;
    }

    public async Task<List<TrendingDestinationResponseDto>> GetTrendingDestinationsAsync()
    {
        var fromDate = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-30);
        return await _trendingDestinationRepository.GetTrendingDestinationsAsync(fromDate);
    }
}
