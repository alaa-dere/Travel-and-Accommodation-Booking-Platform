namespace HotelBooking.Domain.ValueObjects;

public sealed record PendingBookingWindow
{
    public DateTime CreatedAt { get; }
    public DateTime ExpiresAt { get; }

    public PendingBookingWindow(DateTime createdAt, DateTime expiresAt)
    {
        if (createdAt.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Booking creation time must be in UTC.", nameof(createdAt));
        }       
        if (expiresAt.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Pending expiration time must be in UTC.", nameof(expiresAt));
        }        
        if (expiresAt <= createdAt)
        {
            throw new ArgumentException("Pending expiration time must be after the booking creation time.", nameof(expiresAt));
        }
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }
}
