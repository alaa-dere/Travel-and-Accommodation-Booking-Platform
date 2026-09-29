using HotelBooking.Domain.Enums;

namespace HotelBooking.Application.Checkout.Dtos;

public class CheckoutPaymentResultDto
{
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public int PaymentId { get; set; }
    public string? ProviderPaymentId { get; set; }
    public string? ClientSecret { get; set; }
}
