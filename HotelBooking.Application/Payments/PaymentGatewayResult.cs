namespace HotelBooking.Application.Payments;

public sealed record PaymentGatewayResult(
    string ProviderPaymentId,
    string? ClientSecret,
    PaymentGatewayStatus Status,
    string? FailureCode = null);

public enum PaymentGatewayStatus
{
    Pending,
    RequiresAction,
    Succeeded,
    Failed,
    Cancelled
}

public sealed record PaymentProviderWebhookEvent(
    string EventId,
    PaymentProviderWebhookEventType Type,
    string ProviderObjectId,
    PaymentGatewayStatus? PaymentStatus = null,
    RefundGatewayStatus? RefundStatus = null,
    string? FailureCode = null,
    int? InternalPaymentId = null);

public enum PaymentProviderWebhookEventType
{
    Ignored,
    Payment,
    Refund
}

public sealed record RefundGatewayResult(string ProviderRefundId, RefundGatewayStatus Status, string? FailureCode = null);

public enum RefundGatewayStatus
{
    Pending,
    RequiresAction,
    Succeeded,
    Failed,
    Cancelled
}
