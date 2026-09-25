using HotelBooking.Domain.Enums;

namespace HotelBooking.Application.Checkout.Dtos.Confirmation;

public class BookingConfirmationDto
{
    public int ConfirmationId { get; set; }
    public int HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public List<BookingConfirmationRoomDto> Rooms { get; set; } = new();
}