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

    PaymentWebhookEvent ParseWebhook(string payload, string signature);
}
