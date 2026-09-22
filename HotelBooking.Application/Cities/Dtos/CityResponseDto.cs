namespace HotelBooking.Application.Cities;

public class CityResponseDto
{
    public int CityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PostOffice { get; set; } = string.Empty;
}