using HotelBooking.Application.Bookings;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using Moq;

namespace HotelBooking.UnitTests.Bookings;

public class BookingPricingServiceTests
{
    private readonly Mock<IRoomRepository> _roomRepositoryMock;
    private readonly Mock<IPromotionRepository> _promotionRepositoryMock;
    private readonly BookingPricingService _service;

    public BookingPricingServiceTests()
    {
        _roomRepositoryMock = new Mock<IRoomRepository>();
        _promotionRepositoryMock = new Mock<IPromotionRepository>();

        _service = new BookingPricingService(_roomRepositoryMock.Object, _promotionRepositoryMock.Object);
    }

    [Fact]
    public async Task CalculatePriceAsync_WhenCheckOutIsBeforeCheckIn_ShouldThrowBadRequestException()
    {
        // Arrange
        var checkIn = DateTime.UtcNow.Date.AddDays(5);
        var checkOut = checkIn.AddDays(-1);

        // Act
        var action = async () => await _service.CalculatePriceAsync( 1, checkIn, checkOut, DateTime.UtcNow);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        _roomRepositoryMock.Verify(repository => repository.GetRoomByIdAsync(It.IsAny<int>()), Times.Never);
        _promotionRepositoryMock.Verify(repository => repository.GetActivePromotionForHotelAsync(It.IsAny<int>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task CalculatePriceAsync_WhenCheckOutEqualsCheckIn_ShouldThrowBadRequestException()
    {
        // Arrange
        var checkIn = DateTime.UtcNow.Date.AddDays(5);
        var checkOut = checkIn;

        // Act
        var action = async () => await _service.CalculatePriceAsync(1, checkIn, checkOut, DateTime.UtcNow);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        _roomRepositoryMock.Verify(repository => repository.GetRoomByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CalculatePriceAsync_WhenDatesHaveDifferentTimesButSameDate_ShouldThrowBadRequestException()
    {
        // Arrange
        var checkIn = DateTime.UtcNow.Date.AddDays(5).AddHours(10);
        var checkOut = DateTime.UtcNow.Date.AddDays(5).AddHours(18);

        // Act
        var action = async () => await _service.CalculatePriceAsync(1, checkIn, checkOut, DateTime.UtcNow);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        _roomRepositoryMock.Verify(repository => repository.GetRoomByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CalculatePriceAsync_WhenRoomDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        const int roomId = 10;

        var checkIn = DateTime.UtcNow.Date.AddDays(5);
        var checkOut = checkIn.AddDays(2);

        _roomRepositoryMock.Setup(repository => repository.GetRoomByIdAsync(roomId)).ReturnsAsync((Room?)null);

        // Act
        var action = async () => await _service.CalculatePriceAsync(roomId, checkIn, checkOut, DateTime.UtcNow);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);

        _promotionRepositoryMock.Verify(repository => repository.GetActivePromotionForHotelAsync(It.IsAny<int>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task CalculatePriceAsync_WhenNoPromotionExists_ShouldCalculateOriginalPriceWithoutDiscount()
    {
        // Arrange
        const int roomId = 10;
        const int hotelId = 5;

        var checkIn = new DateTime(2026, 10, 10);
        var checkOut = new DateTime(2026, 10, 13);
        var bookingCreationTime = new DateTime(2026, 9, 26);
        var room = CreateRoom(roomId, hotelId, pricePerNight: 100m);

        _roomRepositoryMock.Setup(repository => repository.GetRoomByIdAsync(roomId)).ReturnsAsync(room);
        _promotionRepositoryMock.Setup(repository => repository.GetActivePromotionForHotelAsync(hotelId, bookingCreationTime)).ReturnsAsync((Promotion?)null);

        // Act
        var result = await _service.CalculatePriceAsync(roomId, checkIn, checkOut, bookingCreationTime);

        // Assert
        Assert.Equal(100m, result.PricePerNight);
        Assert.Equal(3, result.NumberOfNights);
        Assert.Equal(300m, result.OriginalTotalPrice);
        Assert.Equal(0, result.DiscountPercentage);
        Assert.Equal(0m, result.DiscountAmount);
        Assert.Equal(300m, result.TotalPrice);
    }

    [Fact]
    public async Task CalculatePriceAsync_WhenPromotionExists_ShouldApplyDiscount()
    {
        // Arrange
        const int roomId = 10;
        const int hotelId = 5;

        var checkIn = new DateTime(2026, 10, 10);
        var checkOut = new DateTime(2026, 10, 13);
        var bookingCreationTime = new DateTime(2026, 9, 26);

        var room = CreateRoom(roomId, hotelId, pricePerNight: 100m);

        var promotion = new Promotion(
            hotelId: hotelId,
            discountPercentage: 20,
            startDate: bookingCreationTime.AddDays(-1),
            endDate: bookingCreationTime.AddDays(5));

        _roomRepositoryMock.Setup(repository => repository.GetRoomByIdAsync(roomId)).ReturnsAsync(room);
        _promotionRepositoryMock.Setup(repository => repository.GetActivePromotionForHotelAsync(hotelId, bookingCreationTime)).ReturnsAsync(promotion);

        // Act
        var result = await _service.CalculatePriceAsync(roomId, checkIn, checkOut, bookingCreationTime);

        // Assert
        Assert.Equal(100m, result.PricePerNight);
        Assert.Equal(3, result.NumberOfNights);
        Assert.Equal(300m, result.OriginalTotalPrice);
        Assert.Equal(20, result.DiscountPercentage);
        Assert.Equal(60m, result.DiscountAmount);
        Assert.Equal(240m, result.TotalPrice);
    }

    [Fact]
    public async Task CalculatePriceAsync_ShouldCalculateNumberOfNightsUsingDatesOnly()
    {
        // Arrange
        const int roomId = 10;
        const int hotelId = 5;

        var checkIn = new DateTime(2026, 10, 10, 20, 0, 0);
        var checkOut = new DateTime(2026, 10, 13, 8, 0, 0);
        var bookingCreationTime = new DateTime(2026, 9, 26);

        var room = CreateRoom(roomId, hotelId, pricePerNight: 120m);

        _roomRepositoryMock.Setup(repository => repository.GetRoomByIdAsync(roomId)).ReturnsAsync(room);
        _promotionRepositoryMock.Setup(repository => repository.GetActivePromotionForHotelAsync(hotelId, bookingCreationTime)).ReturnsAsync((Promotion?)null);

        // Act
        var result = await _service.CalculatePriceAsync(roomId, checkIn, checkOut, bookingCreationTime);

        // Assert
        Assert.Equal(3, result.NumberOfNights);
        Assert.Equal(360m, result.OriginalTotalPrice);
        Assert.Equal(360m, result.TotalPrice);
    }

    [Fact]
    public async Task CalculatePriceAsync_ShouldRequestPromotionForRoomsHotelAtBookingCreationTime()
    {
        // Arrange
        const int roomId = 10;
        const int hotelId = 5;

        var checkIn = new DateTime(2026, 10, 10);
        var checkOut = new DateTime(2026, 10, 12);
        var bookingCreationTime = new DateTime(2026, 9, 26, 14, 30, 0);
        var room = CreateRoom(roomId, hotelId, pricePerNight: 100m);

        _roomRepositoryMock.Setup(repository => repository.GetRoomByIdAsync(roomId)).ReturnsAsync(room);
        _promotionRepositoryMock.Setup(repository => repository.GetActivePromotionForHotelAsync(hotelId, bookingCreationTime)).ReturnsAsync((Promotion?)null);

        // Act
        await _service.CalculatePriceAsync(roomId, checkIn, checkOut, bookingCreationTime);

        // Assert
        _promotionRepositoryMock.Verify(repository => repository.GetActivePromotionForHotelAsync(hotelId, bookingCreationTime), Times.Once);
    }

    private static Room CreateRoom(int roomId, int hotelId, decimal pricePerNight)
    {
        return new Room(
            roomNumber: "101",
            roomType: (RoomType)1,
            pricePerNight: pricePerNight,
            adultsCapacity: 2,
            childCapacity: 2,
            hotelId: hotelId,
            description: null)
        {
            RoomId = roomId
        };
    }
}