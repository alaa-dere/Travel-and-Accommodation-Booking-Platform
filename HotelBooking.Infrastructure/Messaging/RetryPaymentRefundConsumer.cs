using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Messaging;
using HotelBooking.Application.Payments;
using HotelBooking.Domain.Enums;
using MassTransit;

namespace HotelBooking.Infrastructure.Messaging;

public sealed class RetryPaymentRefundConsumer : IConsumer<RetryPaymentRefund>
{
    private readonly IPaymentRepository _payments;
    private readonly IPaymentGateway _gateway;
    private readonly IBookingTransactionManager _transactionManager;
    private readonly TimeProvider _timeProvider;

    public RetryPaymentRefundConsumer(
        IPaymentRepository payments,
        IPaymentGateway gateway,
        IBookingTransactionManager transactionManager,
        TimeProvider timeProvider)
    {
        _payments = payments;
        _gateway = gateway;
        _transactionManager = transactionManager;
        _timeProvider = timeProvider;
    }

    public async Task Consume(ConsumeContext<RetryPaymentRefund> context)
    {
        var payment = await _payments.GetByIdAsync(context.Message.PaymentId)
            ?? throw new InvalidOperationException("Payment was not found.");

        if (payment.Status == PaymentStatus.Refunded)
            return;

        var result = await _gateway.RefundAsync(
            context.Message.ProviderPaymentId,
            $"expired-booking-refund:{context.Message.ProviderPaymentId}",
            context.CancellationToken);

        if (result.Status is RefundGatewayStatus.Failed or RefundGatewayStatus.Cancelled)
        {
            throw new PaymentProviderUnavailableException("Stripe did not accept the refund retry.");
        }

        await _transactionManager.ExecuteSerializableAsync(() =>
        {
            payment.RecordRefund(
                result.ProviderRefundId,
                MapStatus(result.Status),
                result.FailureCode,
                _timeProvider.GetUtcNow().UtcDateTime);
            return Task.CompletedTask;
        });
    }

    private static RefundStatus MapStatus(RefundGatewayStatus status) => status switch
    {
        RefundGatewayStatus.RequiresAction => RefundStatus.RequiresAction,
        RefundGatewayStatus.Succeeded => RefundStatus.Succeeded,
        RefundGatewayStatus.Failed => RefundStatus.Failed,
        RefundGatewayStatus.Cancelled => RefundStatus.Cancelled,
        _ => RefundStatus.Pending
    };
}
