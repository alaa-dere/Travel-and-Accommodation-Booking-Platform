using HotelBooking.Application.Payments.Dtos;

namespace HotelBooking.Application.Checkout.Dtos;

public class CompleteCheckoutRequestDto
{
    public string? SpecialRequests { get; set; }
    public PaymentInformationDto Payment { get; set; } = new();
}