namespace HotelBooking.Application.Hotels.Dtos;

public class HotelListRequestDto
{
    public string? Search { get; set; }
    public int PageNumber { get; set; } = 1;
}
