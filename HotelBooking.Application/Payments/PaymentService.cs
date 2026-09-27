using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Payments.Dtos;
using HotelBooking.Domain.Entities;
using HotelBooking.Application.Exceptions;

namespace HotelBooking.Application.Payments;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentGateway _paymentGateway;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IPaymentGateway paymentGateway)
    {
        _paymentRepository = paymentRepository;
        _paymentGateway = paymentGateway;
    }

    public async Task<Payment> ProcessPaymentAsync(Invoice invoice, PaymentInformationDto paymentInformation)
    {
        var payment = new Payment(invoice.TotalAmount)
        {
            Invoice = invoice
        };

        if (string.IsNullOrWhiteSpace(paymentInformation.PaymentMethodId))
            throw new BadRequestException("A Stripe payment method ID is required.");

        var idempotencyKey = $"checkout-{invoice.UserId}-{invoice.HotelId}-{Guid.NewGuid():N}";
        var gatewayResult = await _paymentGateway.CreateAndConfirmAsync(
            invoice.TotalAmount,
            _paymentGateway.Currency,
            paymentInformation.PaymentMethodId,
            idempotencyKey,
            new Dictionary<string, string>
            {
                ["userId"] = invoice.UserId.ToString(),
                ["hotelId"] = invoice.HotelId.ToString()
            });

        payment.AttachProviderPayment(
            gatewayResult.ProviderPaymentId,
            _paymentGateway.Currency,
            gatewayResult.ClientSecret);

        switch (gatewayResult.Status)
        {
            case PaymentGatewayStatus.Succeeded:
                payment.MarkAsPaid();
                break;
            case PaymentGatewayStatus.Failed:
                payment.MarkAsFailed(gatewayResult.FailureCode);
                break;
            case PaymentGatewayStatus.Cancelled:
                payment.MarkAsCancelled();
                break;
            case PaymentGatewayStatus.RequiresAction:
                payment.MarkAsRequiresAction();
                break;
        }

        await _paymentRepository.AddAsync(payment);
        return payment;
    }
}
