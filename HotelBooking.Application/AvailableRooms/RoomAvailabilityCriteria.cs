namespace HotelBooking.Application.AvailableRooms;

public sealed record RoomAvailabilityCriteria(DateTime CheckIn, DateTime CheckOut, int Adults, int Children);
