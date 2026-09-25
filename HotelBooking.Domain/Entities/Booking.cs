namespace HotelBooking.Domain.Entities;

public class Booking
{
    public int BookingId { get; set; }
    public int UserId { get; set; }
    public int RoomId { get; set; }
    public int InvoiceId { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int Adults { get; set; }
    public int Children { get; set; }
    public decimal PricePerNight  { get; set; }
    public decimal OriginalTotalPrice { get; set; }
    public int DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalPrice { get; set; }
    public BookingStatus BookingStatus  { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? SpecialRequests { get; set; }
    public User? User { get; set; }
    public Room? Room { get; set; }
    public Review? Review { get; set; }
    public Invoice? Invoice { get; set; }

    public Booking(int userId, int roomId, DateTime checkIn, DateTime checkOut, int adults, int children, decimal pricePerNight, decimal originalTotalPrice, int discountPercentage, decimal discountAmount, decimal totalPrice, string? specialRequests)
{
    if (userId <= 0)
    {
        throw new ArgumentOutOfRangeException(nameof(userId), "User ID must be greater than zero.");
    }
    
    if (roomId <= 0)
    {
        throw new ArgumentOutOfRangeException(nameof(roomId), "Room ID must be greater than zero.");
    }
    
    if (checkOut <= checkIn)
    {
        throw new ArgumentException("Check-out date must be after the check-in date.", nameof(checkOut));
    }
    
    if (adults < 1)
    {
        throw new ArgumentOutOfRangeException(nameof(adults), "Number of adults must be at least 1.");
    }
    
    if (children < 0)
    {
        throw new ArgumentOutOfRangeException(nameof(children), "Number of children cannot be negative.");
    }
    
    if (pricePerNight <= 0)
    {
        throw new ArgumentOutOfRangeException(nameof(pricePerNight), "Price per night must be greater than zero.");
    }
    
    if (originalTotalPrice <= 0)
    {
        throw new ArgumentOutOfRangeException(nameof(originalTotalPrice), "Original total price must be greater than zero.");
    }
    
    if (discountPercentage < 0 || discountPercentage >= 100)
    {
        throw new ArgumentOutOfRangeException(nameof(discountPercentage), "Discount percentage must be between 0 and 100.");
    }
    
    if (discountAmount < 0)
    {
        throw new ArgumentOutOfRangeException(nameof(discountAmount), "Discount amount cannot be negative.");
    }
    
    if (totalPrice <= 0)
    {
        throw new ArgumentOutOfRangeException(nameof(totalPrice), "Total price must be greater than zero.");
    }
    
    if (specialRequests?.Length > 1000)
    {
        throw new ArgumentException("Special requests cannot exceed 1000 characters.", nameof(specialRequests));
    }
    
    UserId = userId;
    RoomId = roomId;
    CheckIn = checkIn;
    CheckOut = checkOut;
    Adults = adults;
    Children = children;
    PricePerNight = pricePerNight;
    OriginalTotalPrice = originalTotalPrice;
    DiscountPercentage = discountPercentage;
    DiscountAmount = discountAmount;
    TotalPrice = totalPrice;
    BookingStatus = BookingStatus.Pending;
    CreatedAt = DateTime.UtcNow;
    SpecialRequests = specialRequests;
}
}