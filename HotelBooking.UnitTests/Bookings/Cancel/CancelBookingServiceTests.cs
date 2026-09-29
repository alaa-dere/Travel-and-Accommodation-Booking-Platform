using HotelBooking.Application.Bookings.Cancel;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Payments;
using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Enums;
using Moq;

namespace HotelBooking.UnitTests.Bookings.Cancel;

public class CancelBookingServiceTests
{
    private readonly Mock<IBookingRepository> _bookingRepositoryMock;
    private readonly Mock<IPaymentGateway> _paymentGatewayMock;
    private readonly CancelBookingService _service;

    public CancelBookingServiceTests()
    {
        _bookingRepositoryMock = new Mock<IBookingRepository>();
        _paymentGatewayMock = new Mock<IPaymentGateway>();
        _service = new CancelBookingService(
            _bookingRepositoryMock.Object,
            _paymentGatewayMock.Object,
            TimeProvider.System);
    }

    [Fact]
    public async Task CancelAsync_WhenBookingIdIsInvalid_ShouldThrowBadRequestException()
    {
        // Act
        var action = async () => await _service.CancelAsync(0, 1);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);
    }

    [Fact]
    public async Task CancelAsync_WhenUserIdIsInvalid_ShouldThrowBadRequestException()
    {
        // Act
        var action = async () => await _service.CancelAsync(1, 0);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);
    }

    [Fact]
    public async Task CancelAsync_WhenBookingDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        _bookingRepositoryMock.Setup(repository => repository.GetByIdForUserAsync(1, 1)).ReturnsAsync((Booking?)null);

        // Act
        var action = async () => await _service.CancelAsync(1, 1);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);
    }

    [Fact]
    public async Task CancelAsync_WhenBookingIsAlreadyCancelled_ShouldThrowConflictException()
    {
        // Arrange
        var booking = CreateValidBooking();
        booking.Cancel(booking.CreatedAt);

        _bookingRepositoryMock.Setup(repository => repository.GetByIdForUserAsync(1, 1)).ReturnsAsync(booking);

        // Act
        var action = async () => await _service.CancelAsync(1, 1);

        // Assert
        await Assert.ThrowsAsync<ConflictException>(action);
        _bookingRepositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CancelAsync_WhenStayHasAlreadyStarted_ShouldThrowConflictException()
    {
        // Arrange
        var booking = CreateValidBooking(checkIn: DateTime.UtcNow.AddDays(-1), checkOut: DateTime.UtcNow.AddDays(2));
        _bookingRepositoryMock.Setup(repository => repository.GetByIdForUserAsync(1, 1)).ReturnsAsync(booking);

        // Act
        var action = async () => await _service.CancelAsync(1, 1);

        // Assert
        await Assert.ThrowsAsync<ConflictException>(action);
        _bookingRepositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CancelAsync_WhenBookingIsValid_ShouldCancelBooking()
    {
        // Arrange
        var booking = CreateValidBooking();
        _bookingRepositoryMock.Setup(repository => repository.GetByIdForUserAsync(1, 1)).ReturnsAsync(booking);

        // Act
        await _service.CancelAsync(1, 1);

        // Assert
        Assert.Equal(BookingStatus.Cancelled, booking.BookingStatus);
        Assert.NotNull(booking.UpdatedAt);
    }

    [Fact]
    public async Task CancelAsync_WhenBookingIsValid_ShouldSaveChanges()
    {
        // Arrange
        var booking = CreateValidBooking();
        _bookingRepositoryMock.Setup(repository => repository.GetByIdForUserAsync(1, 1)).ReturnsAsync(booking);

        // Act
        await _service.CancelAsync(1, 1);

        // Assert
        _bookingRepositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CancelAsync_WhenBookingIsPaid_ShouldRequestPartialRefundAndTrackIt()
    {
        var booking = CreateValidBooking();
        AttachPayment(booking, PaymentStatus.Paid);
        _bookingRepositoryMock
            .Setup(repository => repository.GetByIdForUserAsync(1, 1))
            .ReturnsAsync(booking);
        _paymentGatewayMock
            .Setup(gateway => gateway.RefundAsync(
                "pi_test",
                "booking-cancellation:1",
                booking.TotalPrice,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefundGatewayResult("re_partial", RefundGatewayStatus.Succeeded));

        await _service.CancelAsync(1, 1);

        Assert.Equal(BookingStatus.Cancelled, booking.BookingStatus);
        Assert.Equal(booking.TotalPrice, booking.RefundedAmount);
        Assert.Equal("re_partial", booking.ProviderRefundId);
        Assert.Equal(RefundStatus.Succeeded, booking.RefundStatus);
        _paymentGatewayMock.VerifyAll();
    }

    [Fact]
    public async Task CancelAsync_WhenPaymentIsStillProcessing_ShouldThrowConflict()
    {
        var booking = CreateValidBooking();
        AttachPayment(booking, PaymentStatus.Pending);
        _bookingRepositoryMock
            .Setup(repository => repository.GetByIdForUserAsync(1, 1))
            .ReturnsAsync(booking);

        var action = () => _service.CancelAsync(1, 1);

        await Assert.ThrowsAsync<ConflictException>(action);
        Assert.Equal(BookingStatus.Pending, booking.BookingStatus);
        _paymentGatewayMock.Verify(
            gateway => gateway.RefundAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CancelAsync_WhenRefundIsRejected_ShouldNotCancelBooking()
    {
        var booking = CreateValidBooking();
        AttachPayment(booking, PaymentStatus.Paid);
        _bookingRepositoryMock
            .Setup(repository => repository.GetByIdForUserAsync(1, 1))
            .ReturnsAsync(booking);
        _paymentGatewayMock
            .Setup(gateway => gateway.RefundAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefundGatewayResult("re_failed", RefundGatewayStatus.Failed));

        var action = () => _service.CancelAsync(1, 1);

        await Assert.ThrowsAsync<PaymentProviderUnavailableException>(action);
        Assert.Equal(BookingStatus.Pending, booking.BookingStatus);
        _bookingRepositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Never);
    }

    private static Booking CreateValidBooking(DateTime? checkIn = null, DateTime? checkOut = null)
    {
        var booking = BookingTestFactory.Create(
            userId: 1,
            roomId: 1,
            checkIn: checkIn ?? DateTime.UtcNow.AddDays(2),
            checkOut: checkOut ?? DateTime.UtcNow.AddDays(4),
            adults: 2,
            children: 0,
            pricePerNight: 100m,
            originalTotalPrice: 200m,
            discountPercentage: 0,
            discountAmount: 0m,
            totalPrice: 200m,
            specialRequests: null,
            createdAt: DateTime.UtcNow,
            pendingExpiresAt: DateTime.UtcNow.AddHours(1));
        booking.BookingId = 1;
        return booking;
    }

    private static void AttachPayment(Booking booking, PaymentStatus status)
    {
        var invoice = new Invoice(booking.UserId, 1, booking.TotalPrice);
        booking.AssignToInvoice(invoice);
        var payment = new Payment(booking.TotalPrice, DateTime.UtcNow);
        payment.AttachProviderPayment("pi_test", "usd", null);
        if (status == PaymentStatus.Paid)
        {
            payment.MarkAsPaid(DateTime.UtcNow);
        }
        payment.AssignToInvoice(invoice);
    }
}
