namespace HotelBooking.Domain.ValueObjects;

public sealed record BookingStay
{
    public int RoomId { get; }
    public DateTime CheckIn { get; }
    public DateTime CheckOut { get; }
    public int Adults { get; }
    public int Children { get; }

    public BookingStay(int roomId, DateTime checkIn, DateTime checkOut, int adults, int children)
    {
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
        RoomId = roomId;
        CheckIn = checkIn;
        CheckOut = checkOut;
        Adults = adults;
        Children = children;
    }
}
