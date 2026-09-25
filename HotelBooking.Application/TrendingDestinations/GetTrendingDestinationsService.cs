using HotelBooking.Application.Interfaces;
using HotelBooking.Application.TrendingDestinations.Dtos;

namespace HotelBooking.Application.TrendingDestinations;

public class GetTrendingDestinationsService : IGetTrendingDestinationsService
{
    private readonly ITrendingDestinationRepository _trendingDestinationRepository;

    public GetTrendingDestinationsService(ITrendingDestinationRepository trendingDestinationRepository)
    {
        _trendingDestinationRepository = trendingDestinationRepository;
    }

    public async Task<List<TrendingDestinationResponseDto>> GetTrendingDestinationsAsync()
    {
        var fromDate = DateTime.UtcNow.AddDays(-30);
        return await _trendingDestinationRepository.GetTrendingDestinationsAsync(fromDate);
    }
}