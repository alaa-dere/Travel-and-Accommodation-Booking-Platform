using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Search.Dtos;

public class HotelSearchRequestDto
{
    public string Destination { get; set; } = string.Empty;
    public DateTime CheckIn  { get; set; }
    public DateTime CheckOut  { get; set; }
    public int Adults { get; set; }
    public int Children { get; set; }
    public int Rooms { get; set; }
    public decimal? MinPrice  { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? MinRating   { get; set; }
    public List<int>? AmenityIds  { get; set; }
    public HotelType? HotelType { get; set; }
    public RoomType? RoomType { get; set; }
    public int PageNumber { get; set; } = 1;
}