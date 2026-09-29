namespace HotelBooking.Application.Cities;

public class CityListRequestDto
{
    public string? Search { get; set; }
    public int PageNumber { get; set; } = 1;
}
