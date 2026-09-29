namespace HotelBooking.Application.Promotions.Create;

public interface ICreatePromotionService
{
    Task CreateAsync(CreatePromotionRequestDto request);
}