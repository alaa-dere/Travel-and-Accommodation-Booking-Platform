using HotelBooking.Application.Common;
using HotelBooking.Application.Rooms.Dtos;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Interfaces;

public interface IRoomRepository
{
    Task<PagedResult<RoomResponseDto>> GetRoomsAsync(RoomFilterDto filter);
    void Add(Room room);
    Task SaveChangesAsync();
    Task<Room?> GetRoomByIdAsync(int id);
    Task<bool> ExistsAsync(string roomNumber, int hotelId, int? excludedRoomId = null);
}
