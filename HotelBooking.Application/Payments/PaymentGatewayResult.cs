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

public sealed record PaymentWebhookEvent(
    string EventId,
    string ProviderPaymentId,
    PaymentGatewayStatus Status,
    string? FailureCode = null);
