using System.ComponentModel.DataAnnotations;

namespace HotelBooking.Application.HotelImages.Dtos;

public class AddHotelImageRequestDto
{
    [Required]
    [MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int DisplayOrder { get; set; }
}
