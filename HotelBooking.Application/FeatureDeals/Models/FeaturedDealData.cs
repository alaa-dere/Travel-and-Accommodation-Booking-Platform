namespace HotelBooking.Application.FeatureDeals.Models;

public class FeaturedDealData
{
    public int HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public decimal? AverageRating { get; set; }
    public decimal StartingPrice { get; set; }
    public int DiscountPercentage { get; set; }
    public int BookingCountLast30Days { get; set; }
}