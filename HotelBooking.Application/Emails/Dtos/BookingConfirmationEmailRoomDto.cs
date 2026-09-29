namespace HotelBooking.Application.Emails.Dtos;

public class BookingConfirmationEmailRoomDto
{
    public int BookingId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public decimal TotalPrice { get; set; }
}