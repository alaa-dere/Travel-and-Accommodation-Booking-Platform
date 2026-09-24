namespace HotelBooking.Domain.Entities;

public class Booking
{
    public int BookingId { get; set; }
    public int UserId { get; set; }
    public int RoomId { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int Adults { get; set; }
    public int Children { get; set; }
    public decimal PricePerNight  { get; set; }
    public decimal TotalPrice { get; set; }
    public BookingStatus BookingStatus  { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public User? User { get; set; }
    public Room? Room { get; set; }

    public Booking(int userId, int roomId, DateTime checkIn, DateTime checkOut, int adults, int children,
        decimal pricePerNight, decimal totalPrice)
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

        if (totalPrice <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalPrice), "Total price must be greater than zero.");
        }

        UserId = userId;
        RoomId = roomId;
        CheckIn = checkIn;
        CheckOut = checkOut;
        Adults = adults;
        Children = children;
        PricePerNight = pricePerNight;
        TotalPrice = totalPrice;
        BookingStatus = BookingStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }
}