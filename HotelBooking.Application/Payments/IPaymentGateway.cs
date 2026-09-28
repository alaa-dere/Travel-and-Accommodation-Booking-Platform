namespace HotelBooking.Application.Payments;

public interface IPaymentGateway
{
    string Currency { get; }

    Task<PaymentGatewayResult> CreateAndConfirmAsync(
        decimal amount,
        string currency,
        string paymentMethodId,
        string idempotencyKey,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken cancellationToken = default);

    Task<PaymentGatewayStatus> CancelAsync(string providerPaymentId, CancellationToken cancellationToken = default);
    Task<RefundGatewayResult> RefundAsync(string providerPaymentId, string idempotencyKey, CancellationToken cancellationToken = default);
    PaymentProviderWebhookEvent ParseWebhook(string payload, string signature);
}
