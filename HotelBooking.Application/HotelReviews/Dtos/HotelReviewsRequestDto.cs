using System.ComponentModel.DataAnnotations;

namespace HotelBooking.Application.HotelReviews.Dtos;

public class HotelReviewsRequestDto
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;
}
