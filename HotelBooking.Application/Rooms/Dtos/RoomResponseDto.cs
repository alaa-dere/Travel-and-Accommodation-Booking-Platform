using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Rooms.Dtos;

public class RoomResponseDto
{
   public int RoomId { get; set; }
   public int HotelId { get; set; }
   public string RoomNumber { get; set; }  = string.Empty;
   public RoomType RoomType { get; set; }
   public int AdultsCapacity { get; set; }
   public int ChildCapacity { get; set; }
   public decimal PricePerNight { get; set; }
   public bool IsOperationallyAvailable { get; set; }
   public bool IsActive { get; set; }
}