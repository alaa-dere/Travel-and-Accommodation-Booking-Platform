using HotelBooking.Domain.Enums;

namespace HotelBooking.Application.Payments.Dtos;

public class PaymentResultDto
{
    public int PaymentId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
}