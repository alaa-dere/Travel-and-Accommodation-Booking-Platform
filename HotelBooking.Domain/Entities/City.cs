namespace HotelBooking.Domain.Entities;

public class City
{
    public int CityId { get; set; }
    public string Name { get; set; }
    public string Country { get; set; }
    public string PostOffice { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public ICollection<Hotel> Hotels { get; set; } = new List<Hotel>();

    public City(string name, string country, string postOffice)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("City name is required", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(country))
        {
            throw new ArgumentException("City country is required", nameof(country));
        }

        Name = name;
        Country = country;
        PostOffice = postOffice;
        CreatedAt = DateTime.UtcNow;
    }
}