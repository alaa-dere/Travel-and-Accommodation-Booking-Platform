using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;

namespace HotelBooking.Application.Rooms.Images;

public class DeleteRoomImageService : IDeleteRoomImageService
{
    private readonly IRoomRepository _roomRepository;
    private readonly IRoomImageRepository _roomImageRepository;

    public DeleteRoomImageService(IRoomRepository roomRepository, IRoomImageRepository roomImageRepository)
    {
        _roomRepository = roomRepository;
        _roomImageRepository = roomImageRepository;
    }

    public async Task DeleteRoomImageAsync(int roomId, int imageId)
    {
        var room = await _roomRepository.GetRoomByIdAsync(roomId);
        if (room == null)
        {
            throw new NotFoundException("Room not found");
        }
        var image = await _roomImageRepository.GetByIdAsync(imageId);
        if (image == null)
        {
            throw new NotFoundException("Room image not found");
        }

        if (image.RoomId != roomId)
        {
            throw new NotFoundException("Room image not found");
        }

        _roomImageRepository.Delete(image);
        await _roomImageRepository.SaveChangesAsync();
    }
}