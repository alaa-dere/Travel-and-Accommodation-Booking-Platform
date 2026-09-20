namespace HotelBooking.Domain.Entities;

public class Room
{
    public int RoomId { get; set; }
    public string RoomNumber { get; set; }
    public RoomType RoomType { get; set; }
    public int AdultCapacity { get; set; }
    public int ChildCapacity { get; set; }
    public decimal PricePerNight { get; set; }
    public string? Description { get; set; }
    public bool IsOperationallyAvailable { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Room(string roomNumber, RoomType roomType, decimal pricePerNight, int adultsCapacity, int childCapacity)
    {
        if (string.IsNullOrWhiteSpace(roomNumber))
        {
            throw new ArgumentException("Room number cannot be null or empty", nameof(roomNumber));
        }

        if (pricePerNight <= 0)
        {
            throw new ArgumentException("Price per night should be positive", nameof(pricePerNight));
        }

        if (adultsCapacity <= 0)
        {
            throw new ArgumentException("adults capacity cannot be negative or Zero", nameof(adultsCapacity));
        }

        if (childCapacity < 0)
        {
            throw new ArgumentException("child capacity cannot be negative", nameof(childCapacity));
        }
        
        RoomNumber = roomNumber;
        RoomType = roomType;
        PricePerNight = pricePerNight;
        AdultCapacity = adultsCapacity;
        ChildCapacity = childCapacity;
        IsOperationallyAvailable = true;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }
}