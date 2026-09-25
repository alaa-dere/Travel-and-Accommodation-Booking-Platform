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
    
    public Hotel(string name, string ownerName, string address, double latitude, double longitude , HotelType hotelType ,int cityId,string? description,string? history)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(ownerName))
        {
            throw new ArgumentException("OwnerName is required", nameof(ownerName));
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            throw new ArgumentException("Address is required", nameof(address));
        }

        if (cityId <= 0)
        {
            throw new ArgumentException("CityId should be positive", nameof(cityId));
        }
        
        Name = name ;
        OwnerName = ownerName ;
        Address = address ;
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
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(ownerName))
        {
            throw new ArgumentException("OwnerName is required", nameof(ownerName));
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            throw new ArgumentException("Address is required", nameof(address));
        }

        if (cityId <= 0)
        {
            throw new ArgumentException("CityId should be positive", nameof(cityId));
        }
        Name = name ;
        OwnerName = ownerName ;
        Address = address ;
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
}