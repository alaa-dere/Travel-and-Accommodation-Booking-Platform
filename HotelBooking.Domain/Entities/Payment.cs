using HotelBooking.Domain.Enums;

namespace HotelBooking.Domain.Entities;

public class Payment
{
    public int PaymentId { get; internal set; }
    public int InvoiceId { get; internal set; }
    public decimal Amount { get; private set; }
    public PaymentStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string Provider { get; private set; } = "Stripe";
    public string? ProviderPaymentId { get; private set; }
    public string Currency { get; private set; } = "usd";
    public string? ClientSecret { get; private set; }
    public string? FailureCode { get; private set; }
    public string? ProviderRefundId { get; private set; }
    public RefundStatus? RefundStatus { get; private set; }
    public string? RefundFailureCode { get; private set; }
    public Invoice? Invoice { get; private set; }

    private Payment()
    {
    }

    public Payment(decimal amount, DateTime createdAt)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Payment amount must be greater than zero.");
        }

        EnsureUtc(createdAt, nameof(createdAt));

        Amount = amount;
        Status = PaymentStatus.Pending;
        CreatedAt = createdAt;
    }

    public void MarkAsPaid(DateTime processedAt)
    {
        EnsureUtc(processedAt, nameof(processedAt));

        if (Status == PaymentStatus.Paid)
            return;

        if (Status == PaymentStatus.Refunded)
        {
            throw new InvalidOperationException("A refunded payment cannot become paid.");
        }
        Status = PaymentStatus.Paid;
        ProcessedAt = processedAt;
    }

    public void MarkAsFailed(string? failureCode, DateTime processedAt)
    {
        EnsureUtc(processedAt, nameof(processedAt));

        if (Status is PaymentStatus.Paid or PaymentStatus.Cancelled or PaymentStatus.Refunded)
            return;

        FailureCode = failureCode;
        Status = PaymentStatus.Failed;
        ProcessedAt = processedAt;
    }

    public void AttachProviderPayment(string providerPaymentId, string currency, string? clientSecret)
    {
        if (string.IsNullOrWhiteSpace(providerPaymentId))
        {
            throw new ArgumentException("Provider payment ID is required.", nameof(providerPaymentId));
        }
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Payment currency is required.", nameof(currency));
        }
        if (ProviderPaymentId is not null && ProviderPaymentId != providerPaymentId)
        {
            throw new InvalidOperationException("Payment is already linked to another provider payment.");
        }
        ProviderPaymentId = providerPaymentId;
        Currency = currency;
        ClientSecret = clientSecret;
    }

    public void MarkAsRequiresAction()
    {
        if (Status == PaymentStatus.Pending)
            Status = PaymentStatus.RequiresAction;
    }

    public void MarkAsCancelled(DateTime processedAt)
    {
        EnsureUtc(processedAt, nameof(processedAt));

        if (Status is PaymentStatus.Paid or PaymentStatus.Failed or PaymentStatus.Refunded)
            return;

        Status = PaymentStatus.Cancelled;
        ProcessedAt = processedAt;
    }

    public void AssignToInvoice(Invoice invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        if (Invoice is not null && !ReferenceEquals(Invoice, invoice))
        {
            throw new InvalidOperationException("Payment is already assigned to another invoice.");
        }
        Invoice = invoice;
        InvoiceId = invoice.InvoiceId;
        invoice.Payment = this;
    }

    public void RecordRefund(string providerRefundId, RefundStatus refundStatus, string? failureCode, DateTime processedAt)
    {
        if (string.IsNullOrWhiteSpace(providerRefundId))
        {
            throw new ArgumentException("Provider refund ID is required.", nameof(providerRefundId));
        }
        if (processedAt.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Refund processing time must be in UTC.", nameof(processedAt));
        }
        if (Status != PaymentStatus.Paid && RefundStatus is null)
        {
            throw new InvalidOperationException("Only a paid payment can be refunded.");
        }
        ProviderRefundId = providerRefundId;
        RefundStatus = refundStatus;
        RefundFailureCode = failureCode;

        if (refundStatus == Domain.Enums.RefundStatus.Succeeded)
        {
            Status = PaymentStatus.Refunded;
            ProcessedAt = processedAt;
        }
    }

    private static void EnsureUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Payment time must be in UTC.", parameterName);
        }
    }
}
