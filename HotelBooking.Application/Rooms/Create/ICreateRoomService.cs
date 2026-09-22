using HotelBooking.Application.Rooms.Dtos;

namespace HotelBooking.Application.Rooms.Create;

public interface ICreateRoomService
{
    Task<RoomResponseDto> CreateRoomAsync(RoomRequestDto request);
}