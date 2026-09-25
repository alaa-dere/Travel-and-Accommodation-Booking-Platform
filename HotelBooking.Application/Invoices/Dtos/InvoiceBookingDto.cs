namespace HotelBooking.Application.Invoices.Dtos;

public class InvoiceBookingDto
{
    public int BookingId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public decimal PricePerNight { get; set; }
    public decimal OriginalTotalPrice { get; set; }
    public int DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalPrice { get; set; }
}