namespace HotelBooking.Domain.Entities;

public class Invoice
{
    public int InvoiceId { get; set; }
    public int UserId { get; set; }
    public int HotelId { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public User? User { get; set; }
    public Hotel? Hotel { get; set; }
    public Payment? Payment { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public Invoice(int userId, int hotelId, decimal totalAmount)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "User ID must be greater than zero.");
        }
        
        if (hotelId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(hotelId), "Hotel ID must be greater than zero.");
        }
        
        if (totalAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalAmount), "Total amount must be greater than zero.");
        }
        
        UserId = userId;
        HotelId = hotelId;
        TotalAmount = totalAmount;
        CreatedAt = DateTime.UtcNow;
    }
}