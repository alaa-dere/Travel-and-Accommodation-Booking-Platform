namespace HotelBooking.Application.FeatureDeals.Dtos;

public class FeaturedDealResponseDto
{
    public int HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public decimal? Rating { get; set; }
    public decimal OriginalPrice { get; set; }
    public int DiscountPercentage { get; set; }
    public decimal DiscountedPrice { get; set; }
}