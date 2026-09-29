using HotelBooking.Application.FeatureDeals.Dtos;
using HotelBooking.Application.Interfaces;

namespace HotelBooking.Application.FeatureDeals;

public class GetFeaturedDealsService : IFeaturedDealsService
{
    private readonly IFeaturedDealsRepository _featuredDealsRepository;
    private readonly TimeProvider _timeProvider;

    public GetFeaturedDealsService(IFeaturedDealsRepository featuredDealsRepository, TimeProvider timeProvider)
    {
        _featuredDealsRepository = featuredDealsRepository;
        _timeProvider = timeProvider;
    }

    public async Task<IEnumerable<FeaturedDealResponseDto>> GetFeaturedDealsAsync()
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var thirtyDaysAgo = now.AddDays(-30);
        var deals = await _featuredDealsRepository.GetEligibleFeaturedDealsAsync(now, thirtyDaysAgo);

        return deals.Select(deal => new FeaturedDealResponseDto
        {
            HotelId = deal.HotelId,
            HotelName = deal.HotelName,
            City = deal.City,
            Address = deal.Address,
            ThumbnailUrl = deal.ThumbnailUrl,
            Rating = deal.AverageRating,
            OriginalPrice = deal.StartingPrice,
            DiscountPercentage = deal.DiscountPercentage,
            DiscountedPrice = decimal.Round(deal.StartingPrice - (deal.StartingPrice * deal.DiscountPercentage / 100m), 2, MidpointRounding.AwayFromZero)
        });
    }
}
