namespace HotelBooking.Domain.Entities;

public class Hotel
{
    public int HotelId { get; set; }
    public int CityId { get; set; }
    public string Name { get; set; }
    public string OwnerName { get; set; }
    public string? Description { get; set; }
    public string? History { get; set; }
    public string Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public HotelType HotelType { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public City City { get; set; } = null!;
    public ICollection<Room> Rooms { get; set; } = new List<Room>();
    public ICollection<HotelImage> HotelImages { get; set; } = new List<HotelImage>();
    public ICollection<Promotion> Promotions { get; set; } = new List<Promotion>();
    public ICollection<HotelAmenity> HotelAmenities  { get; set; } = new List<HotelAmenity>();
    public ICollection<NearbyAttraction> NearbyAttractions { get; set; } = new List<NearbyAttraction>();
    public ICollection<RecentlyVisitedHotel> RecentlyVisitedHotels { get; set; } = new List<RecentlyVisitedHotel>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    
    public Hotel(string name, string ownerName, string address, double latitude, double longitude , HotelType hotelType ,int cityId,string? description,string? history)
    {
        Validate(name, ownerName, address, latitude, longitude, hotelType, cityId, description, history);
        
        Name = name.Trim();
        OwnerName = ownerName.Trim();
        Address = address.Trim();
        Latitude = latitude;
        Longitude = longitude;
        HotelType = hotelType;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        CityId = cityId;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        History = string.IsNullOrWhiteSpace(history) ? null : history.Trim();
    }
    
    public void Update(string name, string ownerName, string address, double latitude, double longitude , HotelType hotelType ,int cityId,string? description,string? history)
    {
        Validate(name, ownerName, address, latitude, longitude, hotelType, cityId, description, history);

        Name = name.Trim();
        OwnerName = ownerName.Trim();
        Address = address.Trim();
        Latitude = latitude;
        Longitude = longitude;
        HotelType = hotelType;
        UpdatedAt = DateTime.UtcNow;
        CityId = cityId;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        History = string.IsNullOrWhiteSpace(history) ? null : history.Trim();
    }

    public void ChangeStatus(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void Validate(
        string name,
        string ownerName,
        string address,
        double latitude,
        double longitude,
        HotelType hotelType,
        int cityId,
        string? description,
        string? history)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required", nameof(name));
        }

        if (name.Length > 50)
        {
            throw new ArgumentException("Name cannot exceed 50 characters.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(ownerName))
        {
            throw new ArgumentException("OwnerName is required", nameof(ownerName));
        }

        if (ownerName.Length > 50)
        {
            throw new ArgumentException("Owner name cannot exceed 50 characters.", nameof(ownerName));
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            throw new ArgumentException("Address is required", nameof(address));
        }

        if (address.Length > 200)
        {
            throw new ArgumentException("Address cannot exceed 200 characters.", nameof(address));
        }

        if (cityId <= 0)
        {
            throw new ArgumentException("CityId should be positive", nameof(cityId));
        }

        if (!double.IsFinite(latitude) || latitude is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");
        }

        if (!double.IsFinite(longitude) || longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");
        }

        if (!Enum.IsDefined(hotelType))
        {
            throw new ArgumentOutOfRangeException(nameof(hotelType), "Hotel type is invalid.");
        }

        if (description?.Length > 2000)
        {
            throw new ArgumentException("Description cannot exceed 2000 characters.", nameof(description));
        }

        if (history?.Length > 4000)
        {
            throw new ArgumentException("History cannot exceed 4000 characters.", nameof(history));
        }
    }
}
