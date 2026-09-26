using HotelBooking.Application.Bookings;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using Moq;

namespace HotelBooking.UnitTests.Bookings;

public class BookingAvailabilityServiceTests
{
    private readonly Mock<IBookingRepository> _bookingRepositoryMock;
    private readonly BookingAvailabilityService _service;

    public BookingAvailabilityServiceTests()
    {
        _bookingRepositoryMock = new Mock<IBookingRepository>();
        _service = new BookingAvailabilityService(_bookingRepositoryMock.Object);
    }

    [Fact]
    public async Task IsRoomAvailableAsync_WhenRoomIdIsInvalid_ShouldThrowBadRequestException()
    {
        // Arrange
        var checkIn = DateTime.UtcNow.AddDays(2);
        var checkOut = checkIn.AddDays(2);

        // Act
        var action = async () => await _service.IsRoomAvailableAsync( 0, checkIn, checkOut);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        _bookingRepositoryMock.Verify(
            repository => repository.HasConflictingBookingAsync(
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<int?>()),
            Times.Never);
    }

    [Fact]
    public async Task IsRoomAvailableAsync_WhenCheckOutIsBeforeCheckIn_ShouldThrowBadRequestException()
    {
        // Arrange
        var checkIn = DateTime.UtcNow.AddDays(3);
        var checkOut = checkIn.AddDays(-1);

        // Act
        var action = async () => await _service.IsRoomAvailableAsync(1, checkIn, checkOut);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        _bookingRepositoryMock.Verify(
            repository => repository.HasConflictingBookingAsync(
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<int?>()),
            Times.Never);
    }

    [Fact]
    public async Task IsRoomAvailableAsync_WhenCheckOutEqualsCheckIn_ShouldThrowBadRequestException()
    {
        // Arrange
        var checkIn = DateTime.UtcNow.AddDays(3);
        var checkOut = checkIn;

        // Act
        var action = async () => await _service.IsRoomAvailableAsync(1, checkIn, checkOut);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        _bookingRepositoryMock.Verify(
            repository => repository.HasConflictingBookingAsync(
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<int?>()),
            Times.Never);
    }

    [Fact]
    public async Task IsRoomAvailableAsync_WhenBookingConflictExists_ShouldReturnFalse()
    {
        // Arrange
        const int roomId = 1;

        var checkIn = DateTime.UtcNow.AddDays(2);
        var checkOut = checkIn.AddDays(2);

        _bookingRepositoryMock.Setup(repository => repository.HasConflictingBookingAsync(roomId, checkIn, checkOut, null)).ReturnsAsync(true);

        // Act
        var result = await _service.IsRoomAvailableAsync(roomId, checkIn, checkOut);

        // Assert
        Assert.False(result);

        _bookingRepositoryMock.Verify(repository => repository.HasConflictingBookingAsync(roomId, checkIn, checkOut, null), Times.Once);
    }

    [Fact]
    public async Task IsRoomAvailableAsync_WhenBookingConflictDoesNotExist_ShouldReturnTrue()
    {
        // Arrange
        const int roomId = 1;

        var checkIn = DateTime.UtcNow.AddDays(2);
        var checkOut = checkIn.AddDays(2);

        _bookingRepositoryMock.Setup(repository => repository.HasConflictingBookingAsync(roomId, checkIn, checkOut, null)).ReturnsAsync(false);

        // Act
        var result = await _service.IsRoomAvailableAsync(roomId, checkIn, checkOut);

        // Assert
        Assert.True(result);

        _bookingRepositoryMock.Verify(repository => repository.HasConflictingBookingAsync(roomId, checkIn, checkOut, null), Times.Once);
    }

    [Fact]
    public async Task IsRoomAvailableAsync_WhenExcludedBookingIdIsProvided_ShouldPassItToRepository()
    {
        // Arrange
        const int roomId = 1;
        const int excludedBookingId = 10;

        var checkIn = DateTime.UtcNow.AddDays(2);
        var checkOut = checkIn.AddDays(2);

        _bookingRepositoryMock.Setup(repository => repository.HasConflictingBookingAsync(roomId, checkIn, checkOut, excludedBookingId)).ReturnsAsync(false);

        // Act
        var result = await _service.IsRoomAvailableAsync(roomId, checkIn, checkOut, excludedBookingId);

        // Assert
        Assert.True(result);

        _bookingRepositoryMock.Verify(repository => repository.HasConflictingBookingAsync(roomId, checkIn, checkOut, excludedBookingId), Times.Once);
    }
}