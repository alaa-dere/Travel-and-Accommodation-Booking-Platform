namespace HotelBooking.Application.HotelReviews.Dtos;

public class ReviewResponseDto
{
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}