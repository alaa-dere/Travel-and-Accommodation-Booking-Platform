using System.ComponentModel.DataAnnotations;

namespace HotelBooking.Application.Promotions.Create;

public class CreatePromotionRequestDto
{
    [Range(1, int.MaxValue)]
    public int HotelId { get; set; }

    [Range(1, 99)]
    public int DiscountPercentage { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
}
