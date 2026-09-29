using HotelBooking.Application.TrendingDestinations.Dtos;

namespace HotelBooking.Application.Interfaces;

public interface ITrendingDestinationRepository
{
    Task<List<TrendingDestinationResponseDto>> GetTrendingDestinationsAsync(DateTime fromDate);
}