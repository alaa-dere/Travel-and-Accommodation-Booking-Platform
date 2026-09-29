using System.ComponentModel.DataAnnotations;

namespace HotelBooking.Application.HotelReviews.Dtos;

public class SubmitReviewRequestDto
{
    [Range(1, int.MaxValue)]
    public int BookingId { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Comment { get; set; } = string.Empty;
}
