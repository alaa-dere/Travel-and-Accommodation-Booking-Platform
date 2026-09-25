using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Rooms.Delete;

namespace HotelBooking.Application.Rooms.Update;

public class ChangeRoomOperationalAvailability : IChangeRoomOperationalAvailabilityService
{
    private readonly IRoomRepository _roomRepository;

    public ChangeRoomOperationalAvailability(IRoomRepository roomRepository)
    {
        _roomRepository = roomRepository;
    }

    public async Task ChangeOperationalAvailabilityAsync(int roomId, bool isOperationallyAvailable)
    {
        var room = await _roomRepository.GetRoomByIdAsync(roomId);
        if (room == null)
        {
            throw new NotFoundException("Room not found");
        }
        
        room.ChangeOperationalAvailability(isOperationallyAvailable);
        await _roomRepository.SaveChangesAsync();
    }
}