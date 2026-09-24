namespace HotelBooking.Application.Search.Dtos;

public class HotelSearchRequestDto
{
    public string Destination { get; set; } = string.Empty;
    public DateTime CheckIn  { get; set; }
    public DateTime CheckOut  { get; set; }
    public int Adults { get; set; }
    public int Children { get; set; }
    public int Rooms { get; set; }
}