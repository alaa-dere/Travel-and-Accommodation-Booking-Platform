namespace HotelBooking.Domain.Entities;

public class CartItem
{
    public int CartItemId { get; set; }
    public int UserId { get; set; }
    public int RoomId { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int Adults { get; set; }
    public int Children { get; set; }
    public DateTime CreatedAt { get; set; }
    public User? User { get; set; }
    public Room? Room { get; set; }

    public CartItem(int userId, int roomId, DateTime checkIn, DateTime checkOut, int adults, int children)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("User ID must be greater than zero", nameof(userId));
        }

        if (roomId <= 0)
        {
            throw new ArgumentException("Room ID must be greater than zero", nameof(roomId));
        }

        if (checkIn.Date < DateTime.UtcNow.Date)
        {
            throw new ArgumentException("Check-in date cannot be in the past", nameof(checkIn));
        }
        if (checkOut.Date <= checkIn.Date)
        {
            throw new ArgumentException("Check-out date must be after check-in date", nameof(checkOut));
        }
        if (adults <= 0)
        {
            throw new ArgumentException("At least one adult is required", nameof(adults));
        }
        if (children < 0)
        {
            throw new ArgumentException("Children cannot be negative", nameof(children));
        }
        
        UserId = userId;
        RoomId = roomId;
        CheckIn = checkIn;
        CheckOut = checkOut;
        Adults = adults;
        Children = children;
        CreatedAt = DateTime.UtcNow;
    }
}