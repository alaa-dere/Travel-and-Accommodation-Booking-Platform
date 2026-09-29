using System.ComponentModel.DataAnnotations;

namespace HotelBooking.Application.NearbyAttractions.Dtos;

public class CreateNearbyAttractionRequestDto
{
    [Range(1, int.MaxValue)]
    public int HotelId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }
}
