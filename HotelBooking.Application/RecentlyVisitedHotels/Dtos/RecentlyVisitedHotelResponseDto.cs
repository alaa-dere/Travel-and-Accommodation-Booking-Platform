namespace HotelBooking.Application.RecentlyVisitedHotels.Dtos;

public class RecentlyVisitedHotelResponseDto
{
    public int HotelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public double? Rating { get; set; }
    public decimal? StartingPricePerNight { get; set; }
}
