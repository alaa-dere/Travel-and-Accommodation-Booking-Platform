using HotelBooking.Application.FeatureDeals.Models;

namespace HotelBooking.Application.Interfaces;

public interface IFeaturedDealsRepository
{
    Task<IEnumerable<FeaturedDealData>> GetEligibleFeaturedDealsAsync(DateTime now, DateTime thirtyDaysAgo );
}