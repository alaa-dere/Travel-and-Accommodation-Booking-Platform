namespace HotelBooking.Application.HotelLocations.Dtos;

public class NearbyAttractionResponseDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}