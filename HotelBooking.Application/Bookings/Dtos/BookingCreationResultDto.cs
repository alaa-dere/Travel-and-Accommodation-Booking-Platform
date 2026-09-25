using HotelBooking.Application.Checkout.Dtos;

namespace HotelBooking.Application.Bookings.Dtos;

public class BookingCreationResultDto
{
    public int BookingCount { get; set; }
    public int InvoiceCount { get; set; }
    public List<CheckoutPaymentResultDto> Payments { get; set; } = new();
}