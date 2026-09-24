namespace HotelBooking.Domain.Entities;

public class Promotion
{
    public int PromotionId { get; set; }
    public int HotelId { get; set; }
    public int DiscountPercentage { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public Hotel? Hotel { get; set; }

    public Promotion(int hotelId, int discountPercentage, DateTime startDate, DateTime endDate)
    {
        if (hotelId <= 0)
        {
            throw new ArgumentException("Promotion hotelId must be greater than 0");
        }

        if (discountPercentage <= 0 || discountPercentage >= 100)
        {
            throw new ArgumentOutOfRangeException("Promotion discount percentage must be between 0 and 100");
        }

        if (startDate >= endDate)
        {
            throw new ArgumentException("Promotion end date must be greater than start date");
        }
        
        HotelId = hotelId;
        DiscountPercentage = discountPercentage;
        StartDate = startDate;
        EndDate = endDate;
        CreatedAt = DateTime.UtcNow;
    }
}