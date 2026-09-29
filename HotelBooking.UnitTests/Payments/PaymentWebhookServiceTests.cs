using HotelBooking.Application.Emails;
using HotelBooking.Application.Emails.Dtos;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Payments;
using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Enums;
using Moq;

namespace HotelBooking.UnitTests.Payments;

public class PaymentWebhookServiceTests
{
    private static readonly DateTime UtcNow =
        new(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private readonly Mock<IPaymentGateway> _gateway = new();
    private readonly Mock<IPaymentRepository> _payments = new();
    private readonly Mock<IBookingRepository> _bookings = new();
    private readonly Mock<IBookingTransactionManager> _transactionManager = new();
    private readonly Mock<ICartRepository> _cartRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IBookingConfirmationEmailService> _emailService = new();
    private readonly PaymentWebhookService _service;

    public PaymentWebhookServiceTests()
    {
        _gateway.SetupGet(gateway => gateway.Currency).Returns("usd");
        _transactionManager
            .Setup(manager => manager.ExecuteSerializableAsync(It.IsAny<Func<Task>>()))
            .Returns((Func<Task> operation) => operation());
        _cartRepository
            .Setup(repository => repository.GetByUserIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<CartItem>());

        _service = new PaymentWebhookService(
            _gateway.Object,
            _payments.Object,
            _bookings.Object,
            _transactionManager.Object,
            _cartRepository.Object,
            _userRepository.Object,
            _emailService.Object,
            new FixedTimeProvider(UtcNow));
    }

    [Fact]
    public async Task HandleAsync_WhenPaymentSucceeds_ShouldMarkPaymentPaidAndConfirmBooking()
    {
        var (payment, booking) = CreatePendingPayment();
        SetupWebhook(payment, PaymentGatewayStatus.Succeeded);
        _userRepository.Setup(repository => repository.GetEmailByIdAsync(1))
            .ReturnsAsync("customer@test.com");

        await _service.HandleAsync("payload", "signature");

        Assert.Equal(PaymentStatus.Paid, payment.Status);
        Assert.Equal(BookingStatus.Confirmed, booking.BookingStatus);
        _emailService.Verify(service => service.SendAsync(
            It.Is<BookingConfirmationEmailDto>(message =>
                message.CustomerEmail == "customer@test.com" &&
                message.PaymentStatus == PaymentStatus.Paid)), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenPaymentFails_ShouldMarkPaymentFailedAndCancelBooking()
    {
        var (payment, booking) = CreatePendingPayment();
        SetupWebhook(payment, PaymentGatewayStatus.Failed);

        await _service.HandleAsync("payload", "signature");

        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal(BookingStatus.Cancelled, booking.BookingStatus);
        _emailService.Verify(service => service.SendAsync(
            It.IsAny<BookingConfirmationEmailDto>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenSuccessfulPaymentWasAlreadyProcessed_ShouldNotSendDuplicateEmail()
    {
        var (payment, booking) = CreatePendingPayment();
        payment.MarkAsPaid(UtcNow);
        booking.Confirm(UtcNow);
        SetupWebhook(payment, PaymentGatewayStatus.Succeeded);

        await _service.HandleAsync("payload", "signature");

        Assert.Equal(PaymentStatus.Paid, payment.Status);
        Assert.Equal(BookingStatus.Confirmed, booking.BookingStatus);
        _emailService.Verify(service => service.SendAsync(
            It.IsAny<BookingConfirmationEmailDto>()), Times.Never);
        _cartRepository.Verify(repository => repository.DeleteRange(
            It.IsAny<IEnumerable<CartItem>>()), Times.Never);
    }

    [Theory]
    [InlineData(PaymentGatewayStatus.Failed)]
    [InlineData(PaymentGatewayStatus.Cancelled)]
    public async Task HandleAsync_WhenTerminalFailureArrivesAfterSuccess_ShouldNotCancelBooking(
        PaymentGatewayStatus lateStatus)
    {
        var (payment, booking) = CreatePendingPayment();
        payment.MarkAsPaid(UtcNow);
        booking.Confirm(UtcNow);
        SetupWebhook(payment, lateStatus);

        await _service.HandleAsync("payload", "signature");

        Assert.Equal(PaymentStatus.Paid, payment.Status);
        Assert.Equal(BookingStatus.Confirmed, booking.BookingStatus);
    }

    [Theory]
    [InlineData(PaymentGatewayStatus.Failed)]
    [InlineData(PaymentGatewayStatus.Cancelled)]
    public async Task HandleAsync_WhenTerminalFailureIsDeliveredTwice_ShouldBeIdempotent(
        PaymentGatewayStatus status)
    {
        var (payment, booking) = CreatePendingPayment();
        SetupWebhook(payment, status);

        await _service.HandleAsync("payload", "signature");
        await _service.HandleAsync("payload", "signature");

        Assert.Equal(BookingStatus.Cancelled, booking.BookingStatus);
        Assert.Equal(
            status == PaymentGatewayStatus.Failed
                ? PaymentStatus.Failed
                : PaymentStatus.Cancelled,
            payment.Status);
    }

    [Fact]
    public async Task HandleAsync_WhenRefundSucceeds_ShouldMarkPaymentRefunded()
    {
        var (payment, _) = CreatePendingPayment();
        payment.MarkAsPaid(UtcNow);
        payment.RecordRefund("re_test", RefundStatus.Pending, null, UtcNow.AddMinutes(-1));
        _gateway.Setup(gateway => gateway.ParseWebhook("payload", "signature"))
            .Returns(new PaymentProviderWebhookEvent(
                "evt_refund",
                PaymentProviderWebhookEventType.Refund,
                "re_test",
                RefundStatus: RefundGatewayStatus.Succeeded));
        _payments.Setup(repository => repository.GetByProviderRefundIdAsync("re_test"))
            .ReturnsAsync(payment);

        await _service.HandleAsync("payload", "signature");

        Assert.Equal(PaymentStatus.Refunded, payment.Status);
        Assert.Equal(RefundStatus.Succeeded, payment.RefundStatus);
        Assert.Equal(UtcNow, payment.ProcessedAt);
        _emailService.Verify(service => service.SendAsync(
            It.IsAny<BookingConfirmationEmailDto>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenPaymentSucceedsAfterBookingWasCancelled_ShouldRefundWithoutConfirmation()
    {
        var (payment, booking) = CreatePendingPayment();
        booking.Cancel(UtcNow);
        SetupWebhook(payment, PaymentGatewayStatus.Succeeded);
        var transactionActive = false;
        _transactionManager
            .Setup(manager => manager.ExecuteSerializableAsync(It.IsAny<Func<Task>>()))
            .Returns(async (Func<Task> operation) =>
            {
                Assert.False(transactionActive);
                transactionActive = true;
                try
                {
                    await operation();
                }
                finally
                {
                    transactionActive = false;
                }
            });
        _gateway.Setup(gateway => gateway.RefundAsync(
                "pi_test",
                "expired-booking-refund:pi_test",
                It.IsAny<CancellationToken>()))
            .Callback(() => Assert.False(transactionActive))
            .ReturnsAsync(new RefundGatewayResult(
                "re_late_success",
                RefundGatewayStatus.Succeeded));

        await _service.HandleAsync("payload", "signature");

        Assert.Equal(BookingStatus.Cancelled, booking.BookingStatus);
        Assert.Equal(PaymentStatus.Refunded, payment.Status);
        Assert.Equal(RefundStatus.Succeeded, payment.RefundStatus);
        Assert.Equal("re_late_success", payment.ProviderRefundId);
        _emailService.Verify(service => service.SendAsync(
            It.IsAny<BookingConfirmationEmailDto>()), Times.Never);
        _cartRepository.Verify(repository => repository.DeleteRange(
            It.IsAny<IEnumerable<CartItem>>()), Times.Never);
        _transactionManager.Verify(
            manager => manager.ExecuteSerializableAsync(It.IsAny<Func<Task>>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task HandleAsync_WhenProviderIdWasNotSaved_ShouldFindPaymentByMetadataId()
    {
        var (payment, booking) = CreatePendingPayment(attachProviderPayment: false);
        _gateway.Setup(gateway => gateway.ParseWebhook("payload", "signature"))
            .Returns(new PaymentProviderWebhookEvent(
                "evt_test",
                PaymentProviderWebhookEventType.Payment,
                "pi_recovered",
                PaymentStatus: PaymentGatewayStatus.Succeeded,
                InternalPaymentId: payment.PaymentId));
        _payments.Setup(repository => repository.GetByProviderPaymentIdAsync("pi_recovered"))
            .ReturnsAsync((Payment?)null);
        _payments.Setup(repository => repository.GetByIdAsync(payment.PaymentId))
            .ReturnsAsync(payment);

        await _service.HandleAsync("payload", "signature");

        Assert.Equal("pi_recovered", payment.ProviderPaymentId);
        Assert.Equal(PaymentStatus.Paid, payment.Status);
        Assert.Equal(BookingStatus.Confirmed, booking.BookingStatus);
    }

    private void SetupWebhook(Payment payment, PaymentGatewayStatus status)
    {
        _gateway.Setup(gateway => gateway.ParseWebhook("payload", "signature"))
            .Returns(new PaymentProviderWebhookEvent(
                "evt_test",
                PaymentProviderWebhookEventType.Payment,
                "pi_test",
                PaymentStatus: status,
                FailureCode: "declined"));
        _payments.Setup(repository => repository.GetByProviderPaymentIdAsync("pi_test"))
            .ReturnsAsync(payment);
    }

    private static (Payment Payment, Booking Booking) CreatePendingPayment(
        bool attachProviderPayment = true)
    {
        var invoice = new Invoice(userId: 1, hotelId: 10, totalAmount: 200m)
        {
            InvoiceId = 100
        };
        var booking = BookingTestFactory.Create(
            userId: 1,
            roomId: 20,
            checkIn: new DateTime(2030, 1, 10),
            checkOut: new DateTime(2030, 1, 12),
            adults: 2,
            children: 0,
            pricePerNight: 100m,
            originalTotalPrice: 200m,
            discountPercentage: 0,
            discountAmount: 0m,
            totalPrice: 200m,
            specialRequests: null,
            createdAt: UtcNow,
            pendingExpiresAt: UtcNow.AddHours(1));
        booking.BookingId = 200;
        booking.AssignToInvoice(invoice);

        var payment = new Payment(invoice.TotalAmount, UtcNow)
        {
            PaymentId = 300
        };
        payment.AssignToInvoice(invoice);
        if (attachProviderPayment)
            payment.AttachProviderPayment("pi_test", "usd", "pi_test_secret");
        return (payment, booking);
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
