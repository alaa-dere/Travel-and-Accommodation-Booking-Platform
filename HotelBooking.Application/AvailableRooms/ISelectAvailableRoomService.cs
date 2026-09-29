using HotelBooking.Application.AvailableRooms.Dtos;

namespace HotelBooking.Application.AvailableRooms;

public interface ISelectAvailableRoomService
{
    Task<SelectedRoomResponseDto> SelectRoomAsync(int hotelId, int roomId, AvailableRoomsRequestDto request);
}