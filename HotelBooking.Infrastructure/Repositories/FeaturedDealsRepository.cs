using HotelBooking.Application.FeatureDeals.Models;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Repositories;

public class FeaturedDealsRepository : IFeaturedDealsRepository
{
    private readonly HotelBookingDbContext _dbContext;

    public FeaturedDealsRepository(HotelBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<FeaturedDealData>> GetEligibleFeaturedDealsAsync(DateTime now, DateTime thirtyDaysAgo)
    {
        var query = _dbContext.Hotels.AsNoTracking()
            .Where(hotel => hotel.IsActive)
            .Where(hotel => hotel.Promotions.Any(promotion => promotion.IsActive && promotion.StartDate <= now && promotion.EndDate >= now))
            .Where(hotel => hotel.Rooms.Any(room => room.IsActive && room.IsOperationallyAvailable))
            .Select(hotel => new FeaturedDealData
            {
                HotelId = hotel.HotelId,
                HotelName = hotel.Name,
                City = hotel.City.Name,
                Address = hotel.Address,

                StartingPrice = (decimal)hotel.Rooms
                    .Where(room => room.IsActive && room.IsOperationallyAvailable)
                    .Min(room => (double)room.PricePerNight),

                DiscountPercentage = hotel.Promotions
                    .Where(promotion => promotion.IsActive && promotion.StartDate <= now && promotion.EndDate >= now)
                    .OrderByDescending(promotion => promotion.DiscountPercentage)
                    .Select(promotion => promotion.DiscountPercentage)
                    .First(),

                ThumbnailUrl = hotel.HotelImages
                    .OrderBy(image => image.DisplayOrder)
                    .Select(image => image.ImageUrl)
                    .FirstOrDefault(),

                BookingCountLast30Days = hotel.Rooms
                    .SelectMany(room => room.Bookings)
                    .Count(booking => booking.CreatedAt >= thirtyDaysAgo && booking.BookingStatus != BookingStatus.Cancelled),

                AverageRating = (decimal?)hotel.Rooms
                    .SelectMany(room => room.Bookings)
                    .Where(booking => booking.Review != null)
                    .Average(booking => (double?)booking.Review!.Rating)
            })
            .OrderByDescending(deal => deal.BookingCountLast30Days)
            .Take(5);

        return await query.ToListAsync();
    }
}
