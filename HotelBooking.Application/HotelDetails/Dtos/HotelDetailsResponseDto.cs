using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.HotelDetails.Dtos;

public class HotelDetailsResponseDto
{
    public int HotelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? History { get; set; }
    public HotelType HotelType { get; set; }
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Rating { get; set; }
    public List<AmenityResponseDto> Amenities { get; set; } = new();
}