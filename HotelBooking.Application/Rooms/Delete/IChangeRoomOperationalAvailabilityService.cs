namespace HotelBooking.Application.Rooms.Delete;

public interface IChangeRoomOperationalAvailabilityService
{
    Task ChangeOperationalAvailabilityAsync(int roomId, bool isOperationallyAvailable);
}