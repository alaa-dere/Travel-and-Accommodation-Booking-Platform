using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Search;

public class HotelSearchResult
{
    public Hotel Hotel { get; set; } = null!;
    public double? Rating { get; set; }
}