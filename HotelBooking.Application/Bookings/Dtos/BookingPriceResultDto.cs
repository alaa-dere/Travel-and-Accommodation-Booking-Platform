namespace HotelBooking.Application.Bookings.Dtos;

public class BookingPriceResultDto
{
    public decimal PricePerNight { get; set; }
    public int NumberOfNights { get; set; }
    public decimal OriginalTotalPrice { get; set; }
    public int DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalPrice { get; set; }
}