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
}