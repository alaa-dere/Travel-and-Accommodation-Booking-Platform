using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.AvailableRooms.Dtos;

public class AvailableRoomResponseDto
{
    public int RoomId { get; set; }
    public RoomType RoomType { get; set; }
    public string? Description { get; set; }
    public int AdultsCapacity { get; set; }
    public int ChildCapacity { get; set; }
    public decimal PricePerNight { get; set; }
    public List<RoomImageResponseDto> Images { get; set; } = new();
}