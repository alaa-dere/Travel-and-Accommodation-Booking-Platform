using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Enums;

namespace HotelBooking.UnitTests.Domain.Entities;

public class PaymentTests
{
    [Fact]
    public void Constructor_WhenAmountIsValid_ShouldCreatePaymentWithCorrectValues()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var payment = new Payment(500m);

        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(500m, payment.Amount);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Null(payment.ProcessedAt);

        Assert.InRange(
            payment.CreatedAt,
            beforeCreation,
            afterCreation);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenAmountIsInvalid_ShouldThrowArgumentOutOfRangeException(
        int amount)
    {
        // Act
        var action = () =>
            new Payment(amount);

        // Assert
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(action);

        Assert.Equal("amount", exception.ParamName);
    }

    [Fact]
    public void Constructor_WhenAmountIsSmallPositiveValue_ShouldSucceed()
    {
        // Act
        var payment = new Payment(0.01m);

        // Assert
        Assert.Equal(0.01m, payment.Amount);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
    }

    [Fact]
    public void MarkAsPaid_ShouldChangeStatusToPaid()
    {
        // Arrange
        var payment = CreateValidPayment();

        // Act
        payment.MarkAsPaid();

        // Assert
        Assert.Equal(
            PaymentStatus.Paid,
            payment.Status);
    }

    [Fact]
    public void MarkAsPaid_ShouldSetProcessedAt()
    {
        // Arrange
        var payment = CreateValidPayment();

        var beforeProcessing = DateTime.UtcNow;

        // Act
        payment.MarkAsPaid();

        var afterProcessing = DateTime.UtcNow;

        // Assert
        Assert.NotNull(payment.ProcessedAt);

        Assert.InRange(
            payment.ProcessedAt!.Value,
            beforeProcessing,
            afterProcessing);
    }

    [Fact]
    public void MarkAsPaid_ShouldNotChangeAmount()
    {
        // Arrange
        var payment = CreateValidPayment();

        // Act
        payment.MarkAsPaid();

        // Assert
        Assert.Equal(500m, payment.Amount);
    }

    [Fact]
    public void MarkAsFailed_ShouldChangeStatusToFailed()
    {
        // Arrange
        var payment = CreateValidPayment();

        // Act
        payment.MarkAsFailed();

        // Assert
        Assert.Equal(
            PaymentStatus.Failed,
            payment.Status);
    }

    [Fact]
    public void MarkAsFailed_ShouldSetProcessedAt()
    {
        // Arrange
        var payment = CreateValidPayment();

        var beforeProcessing = DateTime.UtcNow;

        // Act
        payment.MarkAsFailed();

        var afterProcessing = DateTime.UtcNow;

        // Assert
        Assert.NotNull(payment.ProcessedAt);

        Assert.InRange(
            payment.ProcessedAt!.Value,
            beforeProcessing,
            afterProcessing);
    }

    [Fact]
    public void MarkAsFailed_ShouldNotChangeAmount()
    {
        // Arrange
        var payment = CreateValidPayment();

        // Act
        payment.MarkAsFailed();

        // Assert
        Assert.Equal(500m, payment.Amount);
    }

    private static Payment CreateValidPayment()
    {
        return new Payment(500m);
    }
}