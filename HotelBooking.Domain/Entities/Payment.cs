using HotelBooking.Domain.Enums;

namespace HotelBooking.Domain.Entities;

public class Payment
{
    public int PaymentId { get; set; }
    public int InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string Provider { get; private set; } = "Stripe";
    public string? ProviderPaymentId { get; private set; }
    public string Currency { get; private set; } = "usd";
    public string? ClientSecret { get; private set; }
    public string? FailureCode { get; private set; }
    public Invoice? Invoice { get; set; }

    public Payment(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Payment amount must be greater than zero.");
        }

        Amount = amount;
        Status = PaymentStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkAsPaid()
    {
        Status = PaymentStatus.Paid;
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed()
    {
        Status = PaymentStatus.Failed;
        ProcessedAt = DateTime.UtcNow;
    }

    public void AttachProviderPayment(string providerPaymentId, string currency, string? clientSecret)
    {
        if (string.IsNullOrWhiteSpace(providerPaymentId))
            throw new ArgumentException("Provider payment ID is required.", nameof(providerPaymentId));

        ProviderPaymentId = providerPaymentId;
        Currency = currency;
        ClientSecret = clientSecret;
    }

    public void MarkAsRequiresAction() => Status = PaymentStatus.RequiresAction;

    public void MarkAsFailed(string? failureCode)
    {
        FailureCode = failureCode;
        MarkAsFailed();
    }

    public void MarkAsCancelled()
    {
        Status = PaymentStatus.Cancelled;
        ProcessedAt = DateTime.UtcNow;
    }
}
