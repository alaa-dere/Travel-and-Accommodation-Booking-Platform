namespace HotelBooking.Domain.Entities;

public class Review
{
    private Review()
    {
    }

    public int ReviewId  { get; set; }
    public int BookingId   { get; set; }
    public int Rating { get; set; }
    public string Comment  { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Booking? Booking { get; set; }

    public Review(int bookingId, int rating, string comment, DateTime? createdAt = null)
    {
        if (bookingId <= 0)
        {
            throw new ArgumentException("Booking id must be greater than zero");
        }
        
        if (rating < 1 || rating > 5)
        {
            throw new ArgumentOutOfRangeException("rating", "rating must be between 1 and 5");
        }

        if (string.IsNullOrWhiteSpace(comment))
        {
            throw new ArgumentException("Comment must not be empty");
        }
        if (comment.Length > 2000)
        {
            throw new ArgumentException("Comment cannot exceed 2000 characters.", nameof(comment));
        }
        var creationTime = createdAt ?? DateTime.UtcNow;
        if (creationTime.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Review creation time must be in UTC.", nameof(createdAt));
        }

        BookingId = bookingId;
        Rating = rating;
        Comment = comment.Trim();
        CreatedAt = creationTime;
    }
}
