namespace HotelBooking.Domain.Entities;

public class City
{
    public int CityId { get; set; }
    public string Name { get; set; }
    public string Country { get; set; }
    public string PostOffice { get; set; }
    public string? ThumbnailUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public ICollection<Hotel> Hotels { get; set; } = new List<Hotel>();

    public City(string name, string country, string postOffice, string? thumbnailUrl = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("City name is required", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(country))
        {
            throw new ArgumentException("City country is required", nameof(country));
        }

        if (string.IsNullOrWhiteSpace(postOffice))
        {
            throw new ArgumentException("City post office is required", nameof(postOffice));
        }

        Name = name.Trim();
        Country = country.Trim();
        PostOffice = postOffice.Trim();
        ThumbnailUrl = ValidateThumbnailUrl(thumbnailUrl);
        CreatedAt = DateTime.UtcNow;
    }
    
    public void Update(string name, string country, string postOffice, string? thumbnailUrl = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("City name is required", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(country))
        {
            throw new ArgumentException("City country is required", nameof(country));
        }

        if (string.IsNullOrWhiteSpace(postOffice))
        {
            throw new ArgumentException("City post office is required", nameof(postOffice));
        }

        Name = name.Trim();
        Country = country.Trim();
        PostOffice = postOffice.Trim();
        ThumbnailUrl = ValidateThumbnailUrl(thumbnailUrl);
        UpdatedAt = DateTime.UtcNow;
    }

    private static string? ValidateThumbnailUrl(string? thumbnailUrl)
    {
        if (thumbnailUrl?.Length > 500)
        {
            throw new ArgumentException("City thumbnail URL cannot exceed 500 characters.", nameof(thumbnailUrl));
        }

        return string.IsNullOrWhiteSpace(thumbnailUrl) ? null : thumbnailUrl.Trim();
    }
}
