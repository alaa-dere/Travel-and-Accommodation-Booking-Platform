namespace HotelBooking.Domain.Entities;

public class RecentlyVisitedHotel
{
    public int RecentlyVisitedHotelId { get; set; }
    public int UserId { get; set; }
    public int HotelId { get; set; }
    public DateTime VisitedAt { get; set; }
    public User? User { get; set; }
    public Hotel? Hotel { get; set; }
}