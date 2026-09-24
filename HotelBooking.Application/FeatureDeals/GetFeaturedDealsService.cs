using HotelBooking.Application.FeatureDeals.Dtos;
using HotelBooking.Application.Interfaces;

namespace HotelBooking.Application.FeatureDeals;

public class GetFeaturedDealsService : IFeaturedDealsService
{
    private readonly IFeaturedDealsRepository  _featuredDealsRepository;

    public GetFeaturedDealsService(IFeaturedDealsRepository featuredDealsRepository)
    {
        _featuredDealsRepository = featuredDealsRepository;
    }
    public async Task<IEnumerable<FeaturedDealResponseDto>> GetFeaturedDealsAsync()
    {
        var now = DateTime.UtcNow;
        var thirtyDaysAgo  = now.AddDays(-30);
        var deals = await _featuredDealsRepository.GetEligibleFeaturedDealsAsync(now, thirtyDaysAgo);
        var result = deals.Select(deal => new FeaturedDealResponseDto
        {
            HotelId = deal.HotelId,
            HotelName = deal.HotelName,
            City = deal.City,
            Address = deal.Address,
            ThumbnailUrl = deal.ThumbnailUrl,
            Rating = deal.AverageRating,
            OriginalPrice = deal.StartingPrice,
            DiscountPercentage = deal.DiscountPercentage,
            DiscountedPrice = deal.StartingPrice - (deal.StartingPrice * deal.DiscountPercentage / 100m)
        });
        return result;
    }
}