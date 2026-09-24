namespace HotelBooking.Domain.Entities;

public class Amenity
{
    public int AmenityId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public ICollection<HotelAmenity> HotelAmenities { get; private set; } = new List<HotelAmenity>();
    
    public Amenity(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Amenity name is required.");
        }

        AmenityId = 0;
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }
}