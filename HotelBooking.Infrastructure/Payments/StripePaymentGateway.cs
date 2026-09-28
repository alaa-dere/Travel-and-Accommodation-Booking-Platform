using HotelBooking.Application.Common.Settings;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Payments;
using Microsoft.Extensions.Options;
using Stripe;

namespace HotelBooking.Infrastructure.Payments;

public sealed class StripePaymentGateway : IPaymentGateway
{
    private readonly StripeSettings _settings;
    private readonly PaymentIntentService _paymentIntents;
    private readonly RefundService _refunds;

    public StripePaymentGateway(IOptions<StripeSettings> settings)
    {
        _settings = settings.Value;
        _paymentIntents = new PaymentIntentService(new StripeClient(_settings.SecretKey));
        _refunds = new RefundService(new StripeClient(_settings.SecretKey));
    }

    public string Currency => _settings.Currency;

    public async Task<PaymentGatewayResult> CreateAndConfirmAsync(
        decimal amount,
        string currency,
        string paymentMethodId,
        string idempotencyKey,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        try
        {
            var intent = await _paymentIntents.CreateAsync(
                new PaymentIntentCreateOptions
                {
                    Amount = ToMinorUnits(amount),
                    Currency = currency,
                    PaymentMethod = paymentMethodId,
                    PaymentMethodTypes = ["card"],
                    Confirm = true,
                    Metadata = new Dictionary<string, string>(metadata)
                },
                new RequestOptions { IdempotencyKey = idempotencyKey }, cancellationToken);

            return new PaymentGatewayResult(intent.Id, intent.ClientSecret, MapStatus(intent.Status), intent.LastPaymentError?.Code);
        }
        catch (StripeException exception) when (exception.StripeError?.Type == "card_error")
        {
            return new PaymentGatewayResult(
                exception.StripeError.PaymentIntent?.Id ?? $"declined-{Guid.NewGuid():N}",
                exception.StripeError.PaymentIntent?.ClientSecret,
                PaymentGatewayStatus.Failed,
                exception.StripeError.Code);
        }
        catch (StripeException exception)
        {
            throw new PaymentProviderUnavailableException("The payment provider is temporarily unavailable. Please try again.", exception);
        }
    }

    public PaymentProviderWebhookEvent ParseWebhook(string payload, string signature)
    {
        EnsureConfigured();

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(payload, signature, _settings.WebhookSecret);
            return stripeEvent.Data.Object switch
            {
                PaymentIntent intent => new PaymentProviderWebhookEvent(
                    stripeEvent.Id,
                    PaymentProviderWebhookEventType.Payment,
                    intent.Id,
                    PaymentStatus: MapStatus(intent.Status),
                    FailureCode: intent.LastPaymentError?.Code,
                    InternalPaymentId: GetInternalPaymentId(intent.Metadata)),
                Refund refund => new PaymentProviderWebhookEvent(
                    stripeEvent.Id,
                    PaymentProviderWebhookEventType.Refund,
                    refund.Id,
                    RefundStatus: MapRefundStatus(refund.Status),
                    FailureCode: refund.FailureReason),
                _ => new PaymentProviderWebhookEvent(
                    stripeEvent.Id,
                    PaymentProviderWebhookEventType.Ignored,
                    string.Empty)
            };
        }
        catch (StripeException exception)
        {
            throw new InvalidPaymentWebhookException("The Stripe webhook signature is invalid.", exception);
        }
    }

    public async Task<PaymentGatewayStatus> CancelAsync(string providerPaymentId, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        if (string.IsNullOrWhiteSpace(providerPaymentId))
        {
            throw new ArgumentException("Provider payment ID is required.", nameof(providerPaymentId));
        }
        try
        {
            var intent = await _paymentIntents.CancelAsync(
                providerPaymentId,
                new PaymentIntentCancelOptions { CancellationReason = "abandoned" },
                cancellationToken: cancellationToken);

            return MapStatus(intent.Status);
        }
        catch (StripeException cancellationException)
        {
            try
            {
                var currentIntent = await _paymentIntents.GetAsync(providerPaymentId, cancellationToken: cancellationToken);

                var currentStatus = MapStatus(currentIntent.Status);
                if (currentStatus is PaymentGatewayStatus.Succeeded or PaymentGatewayStatus.Cancelled)
                    return currentStatus;
            }
            catch (StripeException)
            {
            }

            throw new PaymentProviderUnavailableException("The payment provider could not cancel the payment attempt.", cancellationException);
        }
    }

    public async Task<RefundGatewayResult> RefundAsync(string providerPaymentId, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        try
        {
            var refund = await _refunds.CreateAsync(
                new RefundCreateOptions
                {
                    PaymentIntent = providerPaymentId,
                    Reason = "requested_by_customer"
                },
                new RequestOptions { IdempotencyKey = idempotencyKey }, cancellationToken);

            return new RefundGatewayResult(refund.Id, MapRefundStatus(refund.Status), refund.FailureReason);
        }
        catch (StripeException exception)
        {
            throw new PaymentProviderUnavailableException("The payment provider could not refund the payment.", exception);
        }
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_settings.SecretKey))
        {
            throw new PaymentProviderUnavailableException("Stripe is not configured.");
        }
        
    }

    private static long ToMinorUnits(decimal amount) =>
        decimal.ToInt64(decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero));

    private static int? GetInternalPaymentId(IDictionary<string, string> metadata) =>
        metadata.TryGetValue("paymentId", out var value) && int.TryParse(value, out var paymentId) ? paymentId : null;

    private static PaymentGatewayStatus MapStatus(string status) => status switch
    {
        "succeeded" => PaymentGatewayStatus.Succeeded,
        "requires_action" => PaymentGatewayStatus.RequiresAction,
        "requires_payment_method" => PaymentGatewayStatus.Failed,
        "canceled" => PaymentGatewayStatus.Cancelled,
        _ => PaymentGatewayStatus.Pending
    };

    private static RefundGatewayStatus MapRefundStatus(string status) => status switch
    {
        "succeeded" => RefundGatewayStatus.Succeeded,
        "requires_action" => RefundGatewayStatus.RequiresAction,
        "failed" => RefundGatewayStatus.Failed,
        "canceled" => RefundGatewayStatus.Cancelled,
        _ => RefundGatewayStatus.Pending
    };
}
