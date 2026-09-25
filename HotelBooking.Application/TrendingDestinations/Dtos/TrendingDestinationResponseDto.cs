namespace HotelBooking.Application.TrendingDestinations.Dtos;

public class TrendingDestinationResponseDto
{
    public int CityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public int BookingCount { get; set; }
}