using HotelBooking.Application.Rooms.Dtos;

namespace HotelBooking.Application.Rooms.Images;

public interface IAddRoomImageService
{
    Task AddRoomImageAsync(int roomId, AddRoomImageRequestDto request);
}