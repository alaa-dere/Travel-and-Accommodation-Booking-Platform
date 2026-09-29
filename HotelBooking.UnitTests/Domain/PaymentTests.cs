using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Enums;

namespace HotelBooking.UnitTests.Domain.Entities;

public class PaymentTests
{
    private static readonly DateTime CreatedAt =
        new(2030, 1, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ProcessedAt = CreatedAt.AddMinutes(1);

    [Fact]
    public void Constructor_WhenAmountIsValid_ShouldCreatePendingPaymentAtProvidedTime()
    {
        var payment = new Payment(500m, CreatedAt);

        Assert.Equal(500m, payment.Amount);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(CreatedAt, payment.CreatedAt);
        Assert.Null(payment.ProcessedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenAmountIsInvalid_ShouldThrow(int amount)
    {
        var action = () => new Payment(amount, CreatedAt);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(action);
        Assert.Equal("amount", exception.ParamName);
    }

    [Fact]
    public void Constructor_WhenCreatedAtIsNotUtc_ShouldThrow()
    {
        var action = () => new Payment(500m, DateTime.SpecifyKind(CreatedAt, DateTimeKind.Local));

        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal("createdAt", exception.ParamName);
    }

    [Fact]
    public void MarkAsPaid_ShouldSetStatusAndProvidedProcessingTime()
    {
        var payment = CreateValidPayment();

        payment.MarkAsPaid(ProcessedAt);

        Assert.Equal(PaymentStatus.Paid, payment.Status);
        Assert.Equal(ProcessedAt, payment.ProcessedAt);
        Assert.Equal(500m, payment.Amount);
    }

    [Fact]
    public void MarkAsFailed_ShouldSetFailureInformationAndProvidedProcessingTime()
    {
        var payment = CreateValidPayment();

        payment.MarkAsFailed("card_declined", ProcessedAt);

        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal("card_declined", payment.FailureCode);
        Assert.Equal(ProcessedAt, payment.ProcessedAt);
        Assert.Equal(500m, payment.Amount);
    }

    [Fact]
    public void MarkAsCancelled_ShouldSetProvidedProcessingTime()
    {
        var payment = CreateValidPayment();

        payment.MarkAsCancelled(ProcessedAt);

        Assert.Equal(PaymentStatus.Cancelled, payment.Status);
        Assert.Equal(ProcessedAt, payment.ProcessedAt);
    }

    [Theory]
    [InlineData("paid")]
    [InlineData("failed")]
    [InlineData("cancelled")]
    public void Processing_WhenTimeIsNotUtc_ShouldThrow(string operation)
    {
        var payment = CreateValidPayment();
        var localTime = DateTime.SpecifyKind(ProcessedAt, DateTimeKind.Local);

        Action action = operation switch
        {
            "paid" => () => payment.MarkAsPaid(localTime),
            "failed" => () => payment.MarkAsFailed(null, localTime),
            _ => () => payment.MarkAsCancelled(localTime)
        };

        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void FailedEvent_AfterPaymentWasPaid_ShouldNotDowngradeStatus()
    {
        var payment = CreateValidPayment();
        payment.MarkAsPaid(ProcessedAt);

        payment.MarkAsFailed("late_failure", ProcessedAt.AddMinutes(1));

        Assert.Equal(PaymentStatus.Paid, payment.Status);
        Assert.Equal(ProcessedAt, payment.ProcessedAt);
    }

    [Fact]
    public void CancelledEvent_AfterPaymentWasPaid_ShouldNotDowngradeStatus()
    {
        var payment = CreateValidPayment();
        payment.MarkAsPaid(ProcessedAt);

        payment.MarkAsCancelled(ProcessedAt.AddMinutes(1));

        Assert.Equal(PaymentStatus.Paid, payment.Status);
        Assert.Equal(ProcessedAt, payment.ProcessedAt);
    }

    [Fact]
    public void PaidEvent_AfterLocalCancellation_ShouldRecordProviderSuccess()
    {
        var payment = CreateValidPayment();
        payment.MarkAsCancelled(ProcessedAt);
        var providerSucceededAt = ProcessedAt.AddMinutes(1);

        payment.MarkAsPaid(providerSucceededAt);

        Assert.Equal(PaymentStatus.Paid, payment.Status);
        Assert.Equal(providerSucceededAt, payment.ProcessedAt);
    }

    [Fact]
    public void AttachProviderPayment_WhenAlreadyLinkedToDifferentPayment_ShouldThrow()
    {
        var payment = CreateValidPayment();
        payment.AttachProviderPayment("pi_first", "usd", null);

        var action = () => payment.AttachProviderPayment("pi_second", "usd", null);

        Assert.Throws<InvalidOperationException>(action);
    }

    [Fact]
    public void RecordRefund_WhenPaymentWasNeverPaid_ShouldThrow()
    {
        var payment = CreateValidPayment();

        var action = () => payment.RecordRefund(
            "re_test",
            RefundStatus.Pending,
            null,
            ProcessedAt);

        Assert.Throws<InvalidOperationException>(action);
    }

    private static Payment CreateValidPayment() => new(500m, CreatedAt);
}
