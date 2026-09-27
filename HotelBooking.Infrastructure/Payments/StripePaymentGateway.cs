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

    public StripePaymentGateway(IOptions<StripeSettings> settings)
    {
        _settings = settings.Value;
        _paymentIntents = new PaymentIntentService(new StripeClient(_settings.SecretKey));
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
                new RequestOptions { IdempotencyKey = idempotencyKey },
                cancellationToken);

            return new PaymentGatewayResult(
                intent.Id,
                intent.ClientSecret,
                MapStatus(intent.Status),
                intent.LastPaymentError?.Code);
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
            throw new PaymentProviderUnavailableException(
                "The payment provider is temporarily unavailable. Please try again.", exception);
        }
    }

    public PaymentWebhookEvent ParseWebhook(string payload, string signature)
    {
        EnsureConfigured();

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(payload, signature, _settings.WebhookSecret);
            if (stripeEvent.Data.Object is not PaymentIntent intent)
                return new PaymentWebhookEvent(stripeEvent.Id, string.Empty, PaymentGatewayStatus.Pending);

            return new PaymentWebhookEvent(
                stripeEvent.Id,
                intent.Id,
                MapStatus(intent.Status),
                intent.LastPaymentError?.Code);
        }
        catch (StripeException exception)
        {
            throw new InvalidPaymentWebhookException("The Stripe webhook signature is invalid.", exception);
        }
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_settings.SecretKey))
            throw new PaymentProviderUnavailableException("Stripe is not configured.");
    }

    private static long ToMinorUnits(decimal amount) =>
        decimal.ToInt64(decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero));

    private static PaymentGatewayStatus MapStatus(string status) => status switch
    {
        "succeeded" => PaymentGatewayStatus.Succeeded,
        "requires_action" => PaymentGatewayStatus.RequiresAction,
        "requires_payment_method" => PaymentGatewayStatus.Failed,
        "canceled" => PaymentGatewayStatus.Cancelled,
        _ => PaymentGatewayStatus.Pending
    };
}
