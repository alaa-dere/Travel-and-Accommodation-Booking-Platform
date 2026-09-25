namespace HotelBooking.Application.HotelLocations.Dtos;

public class HotelLocationResponseDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public List<NearbyAttractionResponseDto> Attractions { get; set; } = new();
}