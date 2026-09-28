using HotelBooking.Application.AvailableRooms;
using HotelBooking.Application.AvailableRooms.Dtos;

namespace HotelBooking.Application.Interfaces;

public interface IAvailableRoomRepository
{
    Task<List<AvailableRoomResponseDto>> GetAvailableRoomsAsync(
        int hotelId,
        RoomAvailabilityCriteria criteria);

    Task<AvailableRoomResponseDto?> GetAvailableRoomAsync(
        int hotelId,
        int roomId,
        RoomAvailabilityCriteria criteria);

    Task<AvailableRoomResponseDto?> GetAvailableRoomAsync(
        int roomId,
        RoomAvailabilityCriteria criteria);
}
