using HotelBooking.Application.Payments.Dtos;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Payments;

public interface IPaymentService
{
    Task<Payment> CreatePendingPaymentAsync(Invoice invoice);
    Task ProcessPaymentAsync(Payment payment, PaymentInformationDto paymentInformation);
}
