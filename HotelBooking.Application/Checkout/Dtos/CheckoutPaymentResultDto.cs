using HotelBooking.Domain.Enums;

namespace HotelBooking.Application.Checkout.Dtos;

public class CheckoutPaymentResultDto
{
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
}