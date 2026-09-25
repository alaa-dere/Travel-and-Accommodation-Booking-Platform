namespace HotelBooking.Application.Promotions.Create;

public class CreatePromotionRequestDto
{
    public int HotelId { get; set; }
    public int DiscountPercentage { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}