using System.ComponentModel.DataAnnotations;

namespace HotelBooking.Application.Cities;

public class CityRequestDto
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; }  = string.Empty;
    [Required]
    [MaxLength(50)]
    public string Country { get; set; }  = string.Empty;
    [Required]
    [MaxLength(50)]
    public string PostOffice  { get; set; }  = string.Empty;

    [MaxLength(500)]
    public string? ThumbnailUrl { get; set; }
}
