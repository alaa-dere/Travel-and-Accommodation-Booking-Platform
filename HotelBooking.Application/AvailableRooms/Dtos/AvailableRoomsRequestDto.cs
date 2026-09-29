namespace HotelBooking.Application.AvailableRooms.Dtos;

public class AvailableRoomsRequestDto
{
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int Adults { get; set; }
    public int Children { get; set; }
}