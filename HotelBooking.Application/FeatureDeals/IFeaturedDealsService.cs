using HotelBooking.Application.FeatureDeals.Dtos;

namespace HotelBooking.Application.FeatureDeals;

public interface IFeaturedDealsService
{
    Task<IEnumerable<FeaturedDealResponseDto>> GetFeaturedDealsAsync();
}