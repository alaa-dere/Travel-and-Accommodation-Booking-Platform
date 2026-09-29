namespace HotelBooking.Application.Rooms.Images;

public interface IDeleteRoomImageService
{
    Task DeleteRoomImageAsync(int roomId, int imageId);
}