using HotelBooking.Application.Bookings.Cancel;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Enums;
using Moq;

namespace HotelBooking.UnitTests.Bookings.Cancel;

public class CancelBookingServiceTests
{
    private readonly Mock<IBookingRepository> _bookingRepositoryMock;
    private readonly CancelBookingService _service;

    public CancelBookingServiceTests()
    {
        _bookingRepositoryMock = new Mock<IBookingRepository>();
        _service = new CancelBookingService(_bookingRepositoryMock.Object);
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
        booking.Cancel();

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

    private static Booking CreateValidBooking(DateTime? checkIn = null, DateTime? checkOut = null)
    {
        return new Booking(
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
            specialRequests: null);
    }
}