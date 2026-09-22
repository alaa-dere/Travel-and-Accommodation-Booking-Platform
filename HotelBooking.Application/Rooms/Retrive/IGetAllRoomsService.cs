using HotelBooking.Application.Rooms.Dtos;

namespace HotelBooking.Application.Rooms.Retrive;

public interface IGetAllRoomsService
{
    Task<IEnumerable<RoomResponseDto>> GetAllRoomsAsync(string? search);
}