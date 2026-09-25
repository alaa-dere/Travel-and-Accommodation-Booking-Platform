using HotelBooking.Application.TrendingDestinations.Dtos;

namespace HotelBooking.Application.TrendingDestinations;

public interface IGetTrendingDestinationsService
{
    Task<List<TrendingDestinationResponseDto>> GetTrendingDestinationsAsync();
}