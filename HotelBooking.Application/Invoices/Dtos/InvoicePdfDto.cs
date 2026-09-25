using HotelBooking.Domain.Enums;

namespace HotelBooking.Application.Invoices.Dtos;

public class InvoicePdfDto
{
    public int InvoiceId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<InvoiceBookingDto> Bookings { get; set; } = new();
}