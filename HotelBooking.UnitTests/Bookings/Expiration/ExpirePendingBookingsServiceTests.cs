using HotelBooking.Application.Bookings.Expiration;
using HotelBooking.Application.Common.Settings;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Payments;
using HotelBooking.Application.Messaging;
using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Enums;
using Moq;

namespace HotelBooking.UnitTests.Bookings.Expiration;

public class ExpirePendingBookingsServiceTests
{
    private static readonly DateTime UtcNow =
        new(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ExpireAsync_WhenExpiredBookingsExist_ShouldExpireAndSaveThem()
    {
        var first = CreateBooking(UtcNow.AddMinutes(-2));
        var second = CreateBooking(UtcNow.AddMinutes(-1));
        var repository = new Mock<IBookingRepository>();
        repository
            .Setup(item => item.GetExpiredPendingBookingsAsync(UtcNow, 50))
            .ReturnsAsync([first, second]);
        var service = CreateService(repository.Object, 50);

        var count = await service.ExpireAsync();

        Assert.Equal(2, count);
        Assert.All([first, second], booking =>
        {
            Assert.Equal(BookingStatus.Cancelled, booking.BookingStatus);
            Assert.Equal(UtcNow, booking.UpdatedAt);
        });
        repository.Verify(item => item.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ExpireAsync_WhenNoExpiredBookingsExist_ShouldNotSave()
    {
        var repository = new Mock<IBookingRepository>();
        repository
            .Setup(item => item.GetExpiredPendingBookingsAsync(UtcNow, 100))
            .ReturnsAsync([]);
        var service = CreateService(repository.Object, 100);

        var count = await service.ExpireAsync();

        Assert.Equal(0, count);
        repository.Verify(item => item.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task ExpireAsync_WhenBookingsSharePayment_ShouldCancelProviderOnce()
    {
        var first = CreateBooking(UtcNow.AddMinutes(-2));
        var second = CreateBooking(UtcNow.AddMinutes(-2));
        var payment = AttachPayment(first, second);
        var repository = new Mock<IBookingRepository>();
        repository
            .Setup(item => item.GetExpiredPendingBookingsAsync(UtcNow, 100))
            .ReturnsAsync([first, second]);
        var gateway = new Mock<IPaymentGateway>();
        gateway
            .Setup(item => item.CancelAsync("pi_test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(PaymentGatewayStatus.Cancelled);
        var service = CreateService(repository.Object, 100, gateway.Object);

        var count = await service.ExpireAsync();

        Assert.Equal(2, count);
        Assert.Equal(PaymentStatus.Cancelled, payment.Status);
        Assert.Equal(UtcNow, payment.ProcessedAt);
        gateway.Verify(
            item => item.CancelAsync("pi_test", It.IsAny<CancellationToken>()),
            Times.Once);
        repository.Verify(item => item.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ExpireAsync_WhenProviderReportsSucceeded_ShouldRefundAndExpireBooking()
    {
        var booking = CreateBooking(UtcNow.AddMinutes(-1));
        var payment = AttachPayment(booking);
        var repository = new Mock<IBookingRepository>();
        repository
            .Setup(item => item.GetExpiredPendingBookingsAsync(UtcNow, 100))
            .ReturnsAsync([booking]);
        var gateway = new Mock<IPaymentGateway>();
        gateway
            .Setup(item => item.CancelAsync("pi_test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(PaymentGatewayStatus.Succeeded);
        gateway
            .Setup(item => item.RefundAsync(
                "pi_test",
                "expired-booking-refund:pi_test",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefundGatewayResult("re_test", RefundGatewayStatus.Succeeded));
        var service = CreateService(repository.Object, 100, gateway.Object);

        var count = await service.ExpireAsync();

        Assert.Equal(1, count);
        Assert.Equal(BookingStatus.Cancelled, booking.BookingStatus);
        Assert.Equal(PaymentStatus.Refunded, payment.Status);
        Assert.Equal(RefundStatus.Succeeded, payment.RefundStatus);
        Assert.Equal("re_test", payment.ProviderRefundId);
        repository.Verify(item => item.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ExpireAsync_WhenRefundIsPending_ShouldCancelBookingButKeepPaymentPaid()
    {
        var booking = CreateBooking(UtcNow.AddMinutes(-1));
        var payment = AttachPayment(booking);
        payment.MarkAsPaid(UtcNow);
        var repository = new Mock<IBookingRepository>();
        repository
            .Setup(item => item.GetExpiredPendingBookingsAsync(UtcNow, 100))
            .ReturnsAsync([booking]);
        var gateway = new Mock<IPaymentGateway>();
        gateway
            .Setup(item => item.RefundAsync(
                "pi_test",
                "expired-booking-refund:pi_test",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefundGatewayResult("re_pending", RefundGatewayStatus.Pending));
        var service = CreateService(repository.Object, 100, gateway.Object);

        var count = await service.ExpireAsync();

        Assert.Equal(1, count);
        Assert.Equal(BookingStatus.Cancelled, booking.BookingStatus);
        Assert.Equal(PaymentStatus.Paid, payment.Status);
        Assert.Equal(RefundStatus.Pending, payment.RefundStatus);
        Assert.Equal("re_pending", payment.ProviderRefundId);
        repository.Verify(item => item.SaveChangesAsync(), Times.Once);
    }

    [Theory]
    [InlineData(RefundGatewayStatus.Failed)]
    [InlineData(RefundGatewayStatus.Cancelled)]
    public async Task ExpireAsync_WhenRefundIsRejected_ShouldQueueRetry(
        RefundGatewayStatus refundStatus)
    {
        var booking = CreateBooking(UtcNow.AddMinutes(-1));
        var payment = AttachPayment(booking);
        payment.PaymentId = 42;
        payment.MarkAsPaid(UtcNow);
        var repository = new Mock<IBookingRepository>();
        repository
            .Setup(item => item.GetExpiredPendingBookingsAsync(UtcNow, 100))
            .ReturnsAsync([booking]);
        var gateway = new Mock<IPaymentGateway>();
        gateway.Setup(item => item.RefundAsync(
                "pi_test",
                "expired-booking-refund:pi_test",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefundGatewayResult("re_test", refundStatus));
        var publisher = new Mock<IBackgroundTaskPublisher>();
        var service = CreateService(repository.Object, 100, gateway.Object, publisher.Object);

        await service.ExpireAsync();

        publisher.Verify(item => item.PublishRefundRetryAsync(42, "pi_test"), Times.Once);
        Assert.Equal(BookingStatus.Cancelled, booking.BookingStatus);
    }

    private static ExpirePendingBookingsService CreateService(
        IBookingRepository repository,
        int batchSize,
        IPaymentGateway? paymentGateway = null,
        IBackgroundTaskPublisher? publisher = null)
    {
        return new ExpirePendingBookingsService(
            repository,
            new FixedTimeProvider(UtcNow),
            new BookingSettings { ExpirationBatchSize = batchSize },
            paymentGateway ?? Mock.Of<IPaymentGateway>(),
            publisher ?? Mock.Of<IBackgroundTaskPublisher>());
    }

    private static Payment AttachPayment(params Booking[] bookings)
    {
        var invoice = new Invoice(1, 1, bookings.Sum(item => item.TotalPrice));
        var payment = new Payment(invoice.TotalAmount, UtcNow);
        payment.AttachProviderPayment("pi_test", "usd", null);
        invoice.Payment = payment;

        foreach (var booking in bookings)
        {
            booking.Invoice = invoice;
            invoice.Bookings.Add(booking);
        }

        return payment;
    }

    private static Booking CreateBooking(DateTime expiresAt)
    {
        return BookingTestFactory.Create(
            1,
            1,
            new DateTime(2030, 2, 1),
            new DateTime(2030, 2, 2),
            2,
            0,
            100m,
            100m,
            0,
            0m,
            100m,
            null,
            expiresAt.AddMinutes(-15),
            expiresAt);
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
