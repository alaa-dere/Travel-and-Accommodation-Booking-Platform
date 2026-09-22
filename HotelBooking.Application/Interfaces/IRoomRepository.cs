using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Interfaces;

public interface IRoomRepository
{
    Task<IEnumerable<Room>> GetRoomsAsync(string? search);
    void Add(Room room);
    Task SaveChangesAsync();
    Task<Room?> GetRoomByIdAsync(int id);
    void DeleteRoom(Room room);
}