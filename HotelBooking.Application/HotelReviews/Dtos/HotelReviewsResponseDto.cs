namespace HotelBooking.Application.HotelReviews.Dtos;

public class HotelReviewsResponseDto
{
    public double? Rating { get; set; }
    public List<ReviewResponseDto> Reviews { get; set; } = new();
    public int PageNumber { get; set; }
    public bool HasNextPage { get; set; }
}
