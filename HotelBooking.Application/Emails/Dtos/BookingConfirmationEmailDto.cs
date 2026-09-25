using HotelBooking.Domain.Enums;

namespace HotelBooking.Application.Emails.Dtos;

public class BookingConfirmationEmailDto
{
    public string CustomerEmail { get; set; } = string.Empty;
    public int InvoiceId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public decimal InvoiceTotal { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public List<BookingConfirmationEmailRoomDto> Rooms { get; set; } = new();
}