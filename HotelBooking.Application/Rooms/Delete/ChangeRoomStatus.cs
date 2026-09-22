using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;

namespace HotelBooking.Application.Rooms.Delete;

public class ChangeRoomStatus : IChangeRoomStatusService
{
    private readonly IRoomRepository _roomRepository;
    public ChangeRoomStatus(IRoomRepository  roomRepository)
    {
        _roomRepository = roomRepository;
    }
    public async Task ChangeRoomStatusAsync(int roomId, bool isActive)
    {
        var room = await _roomRepository.GetRoomByIdAsync(roomId);
        if (room == null)
        {
            throw new NotFoundException("Room not found");
        }

        room.ChangeStatus(isActive);
        await _roomRepository.SaveChangesAsync();
    }
}