using HotelBooking.Application.Payments;

namespace HotelBooking.IntegrationTests.Infrastructure;

public sealed class TestPaymentGateway : IPaymentGateway
{
    public string Currency => "usd";

    public Task<PaymentGatewayResult> CreateAndConfirmAsync(
        decimal amount,
        string currency,
        string paymentMethodId,
        string idempotencyKey,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken cancellationToken = default)
    {
        var status = paymentMethodId == "pm_card_declined"
            ? PaymentGatewayStatus.Failed
            : PaymentGatewayStatus.Succeeded;

        return Task.FromResult(new PaymentGatewayResult(
            $"pi_test_{Guid.NewGuid():N}",
            "pi_test_secret",
            status,
            status == PaymentGatewayStatus.Failed ? "card_declined" : null));
    }

    public Task<PaymentGatewayStatus> CancelAsync(
        string providerPaymentId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(PaymentGatewayStatus.Cancelled);
    }

    public Task<RefundGatewayResult> RefundAsync(
        string providerPaymentId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new RefundGatewayResult(
            "re_test",
            RefundGatewayStatus.Succeeded));
    }

    public PaymentProviderWebhookEvent ParseWebhook(string payload, string signature) =>
        new(
            "evt_test",
            PaymentProviderWebhookEventType.Payment,
            "pi_test",
            PaymentStatus: PaymentGatewayStatus.Succeeded);
}
