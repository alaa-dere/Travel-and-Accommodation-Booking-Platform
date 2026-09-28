using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Payments.Dtos;
using HotelBooking.Domain.Entities;
using HotelBooking.Application.Exceptions;

namespace HotelBooking.Application.Payments;

public sealed class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly TimeProvider _timeProvider;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IPaymentGateway paymentGateway,
        TimeProvider timeProvider)
    {
        _paymentRepository = paymentRepository;
        _paymentGateway = paymentGateway;
        _timeProvider = timeProvider;
    }

    public async Task<Payment> CreatePendingPaymentAsync(Invoice invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        var payment = new Payment(invoice.TotalAmount, _timeProvider.GetUtcNow().UtcDateTime);
        payment.AssignToInvoice(invoice);

        await _paymentRepository.AddAsync(payment);
        return payment;
    }

    public async Task ProcessPaymentAsync(Payment payment, PaymentInformationDto paymentInformation)
    {
        ArgumentNullException.ThrowIfNull(payment);

        if (paymentInformation is null)
        {
            throw new BadRequestException("Payment information is required.");
        }
        if (payment.PaymentId <= 0)
        {
            throw new InvalidOperationException("Payment must be saved before it can be processed.");
        }
        if (string.IsNullOrWhiteSpace(paymentInformation.PaymentMethodId))
        {
            throw new BadRequestException("A Stripe payment method ID is required.");
        }
        var invoice = payment.Invoice
            ?? throw new InvalidOperationException("Payment must be associated with an invoice.");
        var idempotencyKey = $"payment-{payment.PaymentId}";
        var gatewayResult = await _paymentGateway.CreateAndConfirmAsync(
            payment.Amount,
            _paymentGateway.Currency,
            paymentInformation.PaymentMethodId,
            idempotencyKey,
            new Dictionary<string, string>
            {
                ["paymentId"] = payment.PaymentId.ToString(),
                ["invoiceId"] = invoice.InvoiceId.ToString(),
                ["userId"] = invoice.UserId.ToString(),
                ["hotelId"] = invoice.HotelId.ToString()
            });

        payment.AttachProviderPayment(gatewayResult.ProviderPaymentId, _paymentGateway.Currency, gatewayResult.ClientSecret);

        var processedAt = _timeProvider.GetUtcNow().UtcDateTime;

        switch (gatewayResult.Status)
        {
            case PaymentGatewayStatus.Pending:
                break;
            case PaymentGatewayStatus.Succeeded:
                payment.MarkAsPaid(processedAt);
                break;
            case PaymentGatewayStatus.Failed:
                payment.MarkAsFailed(gatewayResult.FailureCode, processedAt);
                break;
            case PaymentGatewayStatus.Cancelled:
                payment.MarkAsCancelled(processedAt);
                break;
            case PaymentGatewayStatus.RequiresAction:
                payment.MarkAsRequiresAction();
                break;
        }
    }
}
