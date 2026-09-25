namespace HotelBooking.Application.Bookings.Dtos;

public class ModifyBookingRequestDto
{
    public int RoomId { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int Adults { get; set; }
    public int Children { get; set; }
    public string? SpecialRequests { get; set; }
}