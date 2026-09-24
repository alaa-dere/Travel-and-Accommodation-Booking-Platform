using HotelBooking.Domain.Entities;

public class HotelSearchResponseDto
{
    public int HotelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public HotelType HotelType { get; set; }
    public decimal StartingPrice { get; set; }
    public double? Rating { get; set; }
}