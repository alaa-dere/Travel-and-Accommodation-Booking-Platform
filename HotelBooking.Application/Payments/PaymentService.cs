using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Payments.Dtos;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Payments;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;

    public PaymentService(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<Payment> ProcessPaymentAsync(Invoice invoice, PaymentInformationDto paymentInformation)
    {
        var payment = new Payment(invoice.TotalAmount)
        {
            Invoice = invoice
        };

        if (paymentInformation.ShouldSucceed)
        {
            payment.MarkAsPaid();
        }
        
        else
        {
            payment.MarkAsFailed();
        }

        await _paymentRepository.AddAsync(payment);
        return payment;
    }
}