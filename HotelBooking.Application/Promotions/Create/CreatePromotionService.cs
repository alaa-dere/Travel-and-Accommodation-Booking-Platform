using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Promotions.Create;

public class CreatePromotionService : ICreatePromotionService
{
    private readonly IPromotionRepository _promotionRepository;
    private readonly IHotelRepository _hotelRepository;
    private readonly TimeProvider _timeProvider;

    public CreatePromotionService(IPromotionRepository promotionRepository, IHotelRepository hotelRepository, TimeProvider timeProvider)
    {
        _promotionRepository = promotionRepository;
        _hotelRepository = hotelRepository;
        _timeProvider = timeProvider;
    }

    public CreatePromotionService(IPromotionRepository promotionRepository, IHotelRepository hotelRepository)
        : this(promotionRepository, hotelRepository, TimeProvider.System)
    {
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

        if (await _promotionRepository.HasOverlappingActivePromotionAsync(
                request.HotelId,
                request.StartDate,
                request.EndDate))
        {
            throw new ConflictException("The hotel already has an active promotion during this period.");
        }

        var promotion = new Promotion(
            request.HotelId,
            request.DiscountPercentage,
            request.StartDate,
            request.EndDate,
            _timeProvider.GetUtcNow().UtcDateTime);

        await _promotionRepository.AddAsync(promotion);
        await _promotionRepository.SaveChangesAsync();
    }
}
