using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Repositories;

public class PromotionRepository : IPromotionRepository
{
    private readonly HotelBookingDbContext _dbContext;

    public PromotionRepository(HotelBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Promotion?> GetActivePromotionForHotelAsync(int hotelId, DateTime bookingCreationTime)
    {
        return await _dbContext.Promotions.AsNoTracking()
            .Where(promotion => promotion.HotelId == hotelId && promotion.IsActive && promotion.StartDate <= bookingCreationTime && promotion.EndDate >= bookingCreationTime)
            .OrderByDescending(promotion => promotion.DiscountPercentage)
            .FirstOrDefaultAsync();
    }
    
    public async Task AddAsync(Promotion promotion)
    {
        await _dbContext.Promotions.AddAsync(promotion);
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
    
    public async Task<Promotion?> GetByIdAsync(int promotionId)
    {
        return await _dbContext.Promotions.FirstOrDefaultAsync(promotion => promotion.PromotionId == promotionId);
    }
}