using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Payments;
using HotelBooking.Application.Payments.Dtos;
using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Enums;
using Moq;

namespace HotelBooking.UnitTests.Payments;

public class PaymentServiceTests
{
    private static readonly DateTime UtcNow =
        new(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private readonly Mock<IPaymentRepository> _paymentRepository = new();
    private readonly Mock<IPaymentGateway> _paymentGateway = new();
    private readonly PaymentService _service;

    public PaymentServiceTests()
    {
        _paymentGateway.SetupGet(gateway => gateway.Currency).Returns("usd");
        _paymentGateway
            .Setup(gateway => gateway.CreateAndConfirmAsync(
                It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((decimal _, string _, string paymentMethodId, string _,
                IReadOnlyDictionary<string, string> _, CancellationToken _) =>
                new PaymentGatewayResult(
                    "pi_test",
                    "pi_test_secret",
                    paymentMethodId == "pm_card_declined"
                        ? PaymentGatewayStatus.Failed
                        : PaymentGatewayStatus.Succeeded));

        _service = new PaymentService(
            _paymentRepository.Object,
            _paymentGateway.Object,
            new FixedTimeProvider(UtcNow));
    }

    [Fact]
    public async Task CreatePendingPaymentAsync_ShouldCreateAndAddPendingPayment()
    {
        var invoice = CreateInvoice();

        var payment = await _service.CreatePendingPaymentAsync(invoice);

        Assert.Equal(invoice.TotalAmount, payment.Amount);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Same(invoice, payment.Invoice);
        _paymentRepository.Verify(repository => repository.AddAsync(payment), Times.Once);
        _paymentGateway.Verify(gateway => gateway.CreateAndConfirmAsync(
            It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("pm_card_visa", PaymentStatus.Paid)]
    [InlineData("pm_card_declined", PaymentStatus.Failed)]
    public async Task ProcessPaymentAsync_ShouldApplyProviderResult(
        string paymentMethodId,
        PaymentStatus expectedStatus)
    {
        var payment = CreateSavedPayment();

        await _service.ProcessPaymentAsync(
            payment,
            new PaymentInformationDto { PaymentMethodId = paymentMethodId });

        Assert.Equal(expectedStatus, payment.Status);
        Assert.Equal("pi_test", payment.ProviderPaymentId);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldUseStablePaymentIdForIdempotencyAndMetadata()
    {
        var payment = CreateSavedPayment();

        await _service.ProcessPaymentAsync(
            payment,
            new PaymentInformationDto { PaymentMethodId = "pm_card_visa" });

        _paymentGateway.Verify(gateway => gateway.CreateAndConfirmAsync(
            payment.Amount,
            "usd",
            "pm_card_visa",
            $"payment-{payment.PaymentId}",
            It.Is<IReadOnlyDictionary<string, string>>(metadata =>
                metadata["paymentId"] == payment.PaymentId.ToString() &&
                metadata["invoiceId"] == payment.InvoiceId.ToString()),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenPaymentIsNotSaved_ShouldRejectBeforeCallingProvider()
    {
        var payment = new Payment(350m, UtcNow);
        payment.AssignToInvoice(CreateInvoice());

        var action = () => _service.ProcessPaymentAsync(
            payment,
            new PaymentInformationDto { PaymentMethodId = "pm_card_visa" });

        await Assert.ThrowsAsync<InvalidOperationException>(action);
        _paymentGateway.Verify(gateway => gateway.CreateAndConfirmAsync(
            It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenPaymentMethodIsMissing_ShouldThrowBadRequest()
    {
        var payment = CreateSavedPayment();

        var action = () => _service.ProcessPaymentAsync(
            payment,
            new PaymentInformationDto { PaymentMethodId = " " });

        await Assert.ThrowsAsync<BadRequestException>(action);
    }

    [Theory]
    [InlineData(PaymentGatewayStatus.Pending, PaymentStatus.Pending)]
    [InlineData(PaymentGatewayStatus.RequiresAction, PaymentStatus.RequiresAction)]
    public async Task ProcessPaymentAsync_ShouldPreserveNonFinalProviderStatus(
        PaymentGatewayStatus gatewayStatus,
        PaymentStatus expectedStatus)
    {
        var payment = CreateSavedPayment();
        _paymentGateway
            .Setup(gateway => gateway.CreateAndConfirmAsync(
                It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentGatewayResult(
                "pi_test",
                "pi_test_secret",
                gatewayStatus));

        await _service.ProcessPaymentAsync(
            payment,
            new PaymentInformationDto { PaymentMethodId = "pm_card_visa" });

        Assert.Equal(expectedStatus, payment.Status);
        Assert.Null(payment.ProcessedAt);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenInformationIsNull_ShouldThrowBadRequest()
    {
        var action = () => _service.ProcessPaymentAsync(CreateSavedPayment(), null!);

        await Assert.ThrowsAsync<BadRequestException>(action);
    }

    private static Payment CreateSavedPayment()
    {
        var invoice = CreateInvoice();
        var payment = new Payment(invoice.TotalAmount, UtcNow)
        {
            PaymentId = 25,
        };
        payment.AssignToInvoice(invoice);
        return payment;
    }

    private static Invoice CreateInvoice() => new(1, 10, 350m) { InvoiceId = 100 };

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
