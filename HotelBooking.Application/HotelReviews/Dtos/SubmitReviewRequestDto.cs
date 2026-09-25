namespace HotelBooking.Application.HotelReviews.Dtos;

public class SubmitReviewRequestDto
{
    public int BookingId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
}