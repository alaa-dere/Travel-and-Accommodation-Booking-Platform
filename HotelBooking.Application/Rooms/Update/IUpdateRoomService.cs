using HotelBooking.Application.Rooms.Dtos;

namespace HotelBooking.Application.Rooms.Update;

public interface IUpdateRoomService
{
    Task<RoomResponseDto> UpdateRoomAsync(int id, RoomRequestDto request);

}