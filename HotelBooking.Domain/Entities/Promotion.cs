namespace HotelBooking.Domain.Entities;

public class Promotion
{
    private Promotion()
    {
    }

    public int PromotionId { get; set; }
    public int HotelId { get; set; }
    public int DiscountPercentage { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public Hotel? Hotel { get; set; }
    public bool IsActive { get; set; }

    public Promotion(int hotelId, int discountPercentage, DateTime startDate, DateTime endDate)
        : this(hotelId, discountPercentage, startDate, endDate, DateTime.UtcNow)
    {
    }

    public Promotion(int hotelId, int discountPercentage, DateTime startDate, DateTime endDate, DateTime createdAt)
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
        StartDate = NormalizeUtc(startDate);
        EndDate = NormalizeUtc(endDate);
        CreatedAt = NormalizeUtc(createdAt);
        IsActive = true;
    }
    
    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    private static DateTime NormalizeUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
