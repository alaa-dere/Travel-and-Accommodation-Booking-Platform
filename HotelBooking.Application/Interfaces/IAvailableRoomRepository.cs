using HotelBooking.Application.AvailableRooms.Dtos;

namespace HotelBooking.Application.Interfaces;

public interface IAvailableRoomRepository
{
    Task<List<AvailableRoomResponseDto>> GetAvailableRoomsAsync(int hotelId, DateTime checkIn, DateTime checkOut, int adults, int children);
    Task<AvailableRoomResponseDto?> GetAvailableRoomAsync(int hotelId,int roomId, DateTime checkIn, DateTime checkOut, int adults, int children);
    Task<AvailableRoomResponseDto?> GetAvailableRoomAsync(int roomId, DateTime checkIn, DateTime checkOut, int adults, int children);
}