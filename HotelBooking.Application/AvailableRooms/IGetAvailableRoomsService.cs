using HotelBooking.Application.AvailableRooms.Dtos;

namespace HotelBooking.Application.AvailableRooms;

public interface IGetAvailableRoomsService
{
    Task<List<AvailableRoomResponseDto>> GetAvailableRoomsAsync(int hotelId, AvailableRoomsRequestDto request);
}