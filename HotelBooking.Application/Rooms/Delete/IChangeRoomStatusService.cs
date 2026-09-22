namespace HotelBooking.Application.Rooms.Delete;

public interface IChangeRoomStatusService
{
    Task ChangeRoomStatusAsync(int roomId, bool isActive);
}