namespace HotelBooking.Application.Promotions.Status;

public interface IChangePromotionStatusService
{
    Task ChangeStatusAsync(int promotionId, bool isActive);
}