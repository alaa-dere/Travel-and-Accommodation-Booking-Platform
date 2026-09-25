using HotelBooking.Application.Payments.Dtos;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Payments;

public interface IPaymentService
{
    Task<Payment> ProcessPaymentAsync(Invoice invoice, PaymentInformationDto paymentInformation);
}