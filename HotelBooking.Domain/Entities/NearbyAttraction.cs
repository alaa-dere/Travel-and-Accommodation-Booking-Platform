namespace HotelBooking.Domain.Entities;

public class NearbyAttraction
{
    public int NearbyAttractionId { get; set; }
    public int HotelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public Hotel? Hotel { get; set; }

    public NearbyAttraction(int hotelId, string name, string? description, double latitude, double longitude)
    {
        Validate(hotelId, name,description, latitude, longitude);

        HotelId = hotelId;
        Name = name.Trim();
        Description = description?.Trim();
        Latitude = latitude;
        Longitude = longitude;
    }

    public void Update(string name, string? description, double latitude, double longitude)
    {
        Validate(HotelId, name, description,latitude, longitude);

        Name = name.Trim();
        Description = description?.Trim();
        Latitude = latitude;
        Longitude = longitude;
    }

    private static void Validate(int hotelId, string name, string? description, double latitude, double longitude)
    { 
        if (hotelId <= 0)
        {
            throw new ArgumentException("Hotel ID must be greater than zero.", nameof(hotelId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Attraction name is required.", nameof(name));
        }

        if (latitude < -90 || latitude > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");
        }

        if (longitude < -180 || longitude > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");
        }
        
        if (name.Length > 100)
        {
            throw new ArgumentException("Attraction name cannot exceed 100 characters.", nameof(name));
        }

        if (description?.Length > 500)
        {
            throw new ArgumentException("Attraction description cannot exceed 500 characters.", nameof(description));
        }
    }
}