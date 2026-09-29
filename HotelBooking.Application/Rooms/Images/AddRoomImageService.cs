using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Rooms.Dtos;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Rooms.Images;

public class AddRoomImageService : IAddRoomImageService
{
    private readonly IRoomRepository _roomRepository;
    private readonly IRoomImageRepository _roomImageRepository;

    public AddRoomImageService(IRoomRepository roomRepository, IRoomImageRepository roomImageRepository)
    {
        _roomRepository = roomRepository;
        _roomImageRepository = roomImageRepository;
    }

    public async Task AddRoomImageAsync(int roomId, AddRoomImageRequestDto request)
    {
        var room = await _roomRepository.GetRoomByIdAsync(roomId);
        if (room == null)
        {
            throw new NotFoundException("Room not found");
        }

        var imageUrl = request.ImageUrl.Trim();
        if (await _roomImageRepository.ExistsAsync(roomId, imageUrl, request.DisplayOrder))
        {
            throw new ConflictException("The image URL or display order already exists for this room.");
        }

        var roomImage = new RoomImage(imageUrl, request.DisplayOrder, roomId);
        await _roomImageRepository.AddAsync(roomImage);
        await _roomImageRepository.SaveChangesAsync();
    }
}
