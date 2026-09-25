namespace HotelBooking.Application.HotelReviews.Dtos;

public class HotelReviewsResponseDto
{
    public double? Rating { get; set; }
    public List<ReviewResponseDto> Reviews { get; set; } = new();
}