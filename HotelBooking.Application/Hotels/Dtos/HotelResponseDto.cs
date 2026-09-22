using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Hotels.Dtos;

public class HotelResponseDto
{
    public int HotelId { get; set; }
    public int CityId { get; set; }
    public string Name { get; set; }
    public string OwnerName { get; set; }
    public string Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public HotelType HotelType { get; set; }
}