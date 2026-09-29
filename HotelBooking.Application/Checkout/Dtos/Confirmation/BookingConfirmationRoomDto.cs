namespace HotelBooking.Application.Checkout.Dtos.Confirmation;

public class BookingConfirmationRoomDto
{
    public int BookingId { get; set; }
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public decimal TotalAmount { get; set; }
}