using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Rooms.Dtos;

public class RoomFilterDto
{
    public string? Search { get; set; }
    public int? HotelId { get; set; }
    public RoomType? RoomType { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsOperationallyAvailable { get; set; }
}