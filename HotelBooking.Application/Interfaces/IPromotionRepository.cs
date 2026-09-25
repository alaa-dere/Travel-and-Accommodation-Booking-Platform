using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Interfaces;

public interface IPromotionRepository
{
    Task<Promotion?> GetActivePromotionForHotelAsync(int hotelId, DateTime bookingCreationTime);
    Task<Promotion?> GetByIdAsync(int promotionId);
    Task AddAsync(Promotion promotion);
    Task SaveChangesAsync();
}