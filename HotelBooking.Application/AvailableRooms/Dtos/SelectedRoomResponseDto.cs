using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.AvailableRooms.Dtos;

public class SelectedRoomResponseDto
{
    public int RoomId { get; set; }
    public RoomType RoomType { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int Adults { get; set; }
    public int Children { get; set; }
    public decimal PricePerNight { get; set; }
    public decimal TotalPrice { get; set; }
}