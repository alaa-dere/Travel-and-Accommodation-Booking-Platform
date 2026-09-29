using HotelBooking.Application.Common;
using HotelBooking.Application.Rooms.Dtos;

namespace HotelBooking.Application.Rooms.Retrive;

public interface IGetAllRoomsService
{
    Task<PagedResult<RoomResponseDto>> GetAllRoomsAsync(RoomFilterDto filter);
}
