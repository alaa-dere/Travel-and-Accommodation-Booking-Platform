using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Promotions.Create;

public class CreatePromotionService : ICreatePromotionService
{
    private readonly IPromotionRepository _promotionRepository;
    private readonly IHotelRepository _hotelRepository;

    public CreatePromotionService(IPromotionRepository promotionRepository, IHotelRepository hotelRepository)
    {
        _promotionRepository = promotionRepository;
        _hotelRepository = hotelRepository;
    }

    public async Task CreateAsync(CreatePromotionRequestDto request)
    {
        if (request.HotelId <= 0)
        {
            throw new BadRequestException("Invalid hotel ID.");
        }

        if (request.DiscountPercentage <= 0 || request.DiscountPercentage >= 100)
        {
            throw new BadRequestException("Discount percentage must be between 1 and 99.");
        }

        if (request.StartDate >= request.EndDate)
        {
            throw new BadRequestException("End date must be after start date.");
        }

        var hotel = await _hotelRepository.GetHotelByIdAsync(request.HotelId);

        if (hotel == null)
        {
            throw new NotFoundException("Hotel not found.");
        }

        var promotion = new Promotion(request.HotelId, request.DiscountPercentage, request.StartDate, request.EndDate);

        await _promotionRepository.AddAsync(promotion);
        await _promotionRepository.SaveChangesAsync();
    }
}