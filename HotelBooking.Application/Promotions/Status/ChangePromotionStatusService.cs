using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;

namespace HotelBooking.Application.Promotions.Status;

public class ChangePromotionStatusService : IChangePromotionStatusService
{
    private readonly IPromotionRepository _promotionRepository;

    public ChangePromotionStatusService(IPromotionRepository promotionRepository)
    {
        _promotionRepository = promotionRepository;
    }

    public async Task ChangeStatusAsync(int promotionId, bool isActive)
    {
        if (promotionId <= 0)
        {
            throw new BadRequestException("Invalid promotion ID.");
        }

        var promotion = await _promotionRepository.GetByIdAsync(promotionId);
        if (promotion == null)
        {
            throw new NotFoundException("Promotion not found.");
        }

        if (isActive)
        {
            promotion.Activate();
        }
        else
        {
            promotion.Deactivate();
        }

        await _promotionRepository.SaveChangesAsync();
    }
}