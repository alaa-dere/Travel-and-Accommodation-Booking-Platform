using HotelBooking.Application.AvailableRooms;
using HotelBooking.Application.AvailableRooms.Dtos;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using Moq;

namespace HotelBooking.UnitTests.AvailableRooms;

public class SelectAvailableRoomTests
{
    private readonly Mock<IAvailableRoomRepository> _availableRoomRepositoryMock;
    private readonly SelectAvailableRoom _service;

    public SelectAvailableRoomTests()
    {
        _availableRoomRepositoryMock = new Mock<IAvailableRoomRepository>();
        _service = new SelectAvailableRoom(_availableRoomRepositoryMock.Object);
    }

    [Fact]
    public async Task SelectRoomAsync_WhenCheckOutIsBeforeCheckIn_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.CheckOut = request.CheckIn.AddDays(-1);

        // Act
        var action = async () => await _service.SelectRoomAsync(1, 1, request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        _availableRoomRepositoryMock.Verify(
            repository => repository.GetAvailableRoomAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>(),
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task SelectRoomAsync_WhenCheckOutEqualsCheckIn_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.CheckOut = request.CheckIn;

        // Act
        var action = async () => await _service.SelectRoomAsync(1, 1, request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        _availableRoomRepositoryMock.Verify(
            repository => repository.GetAvailableRoomAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>(),
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task SelectRoomAsync_WhenAdultsIsZero_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Adults = 0;

        // Act
        var action = async () => await _service.SelectRoomAsync(1, 1, request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        _availableRoomRepositoryMock.Verify(
            repository => repository.GetAvailableRoomAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>(),
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task SelectRoomAsync_WhenAdultsIsNegative_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Adults = -1;

        // Act
        var action = async () => await _service.SelectRoomAsync(1, 1, request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        _availableRoomRepositoryMock.Verify(
            repository => repository.GetAvailableRoomAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>(),
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task SelectRoomAsync_WhenChildrenIsNegative_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Children = -1;

        // Act
        var action = async () => await _service.SelectRoomAsync(1, 1, request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        _availableRoomRepositoryMock.Verify(
            repository => repository.GetAvailableRoomAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>(),
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task SelectRoomAsync_WhenRoomIsNotAvailable_ShouldThrowBadRequestException()
    {
        // Arrange
        const int hotelId = 1;
        const int roomId = 10;

        var request = CreateValidRequest();

        _availableRoomRepositoryMock
            .Setup(repository => repository.GetAvailableRoomAsync(
                    hotelId,
                    roomId,
                    request.CheckIn,
                    request.CheckOut,
                    request.Adults,
                    request.Children))
            .ReturnsAsync((AvailableRoomResponseDto?)null);

        // Act
        var action = async () => await _service.SelectRoomAsync(hotelId, roomId, request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        _availableRoomRepositoryMock.Verify(repository => repository.GetAvailableRoomAsync(
                hotelId,
                roomId,
                request.CheckIn,
                request.CheckOut,
                request.Adults,
                request.Children),
            Times.Once);
    }

    [Fact]
    public async Task SelectRoomAsync_WhenRoomIsAvailable_ShouldReturnSelectedRoom()
    {
        // Arrange
        const int hotelId = 1;
        const int roomId = 10;

        var request = CreateValidRequest();

        var room = new AvailableRoomResponseDto
        {
            RoomId = roomId,
            PricePerNight = 100m
        };

        _availableRoomRepositoryMock
            .Setup(repository => repository.GetAvailableRoomAsync(
                    hotelId,
                    roomId,
                    request.CheckIn,
                    request.CheckOut,
                    request.Adults,
                    request.Children))
            .ReturnsAsync(room);

        // Act
        var result = await _service.SelectRoomAsync(hotelId, roomId, request);

        // Assert
        Assert.Equal(roomId, result.RoomId);
        Assert.Equal(room.RoomType, result.RoomType);
        Assert.Equal(request.CheckIn, result.CheckIn);
        Assert.Equal(request.CheckOut, result.CheckOut);
        Assert.Equal(request.Adults, result.Adults);
        Assert.Equal(request.Children, result.Children);
        Assert.Equal(room.PricePerNight, result.PricePerNight);

        _availableRoomRepositoryMock.Verify(repository => repository.GetAvailableRoomAsync(
                hotelId,
                roomId,
                request.CheckIn,
                request.CheckOut,
                request.Adults,
                request.Children),
            Times.Once);
    }

    [Fact]
    public async Task SelectRoomAsync_WhenRoomIsAvailable_ShouldCalculateTotalPriceBasedOnNumberOfNights()
    {
        // Arrange
        const int hotelId = 1;
        const int roomId = 10;

        var request = new AvailableRoomsRequestDto
        {
            CheckIn = new DateTime(2026, 10, 10),
            CheckOut = new DateTime(2026, 10, 13),
            Adults = 2,
            Children = 0
        };

        var room = new AvailableRoomResponseDto
        {
            RoomId = roomId,
            PricePerNight = 100m
        };

        _availableRoomRepositoryMock
            .Setup(repository => repository.GetAvailableRoomAsync(
                    hotelId,
                    roomId,
                    request.CheckIn,
                    request.CheckOut,
                    request.Adults,
                    request.Children))
            .ReturnsAsync(room);

        // Act
        var result = await _service.SelectRoomAsync(hotelId, roomId, request);

        // Assert
        Assert.Equal(300m, result.TotalPrice);
    }

    private static AvailableRoomsRequestDto CreateValidRequest()
    {
        return new AvailableRoomsRequestDto
        {
            CheckIn = DateTime.UtcNow.AddDays(2),
            CheckOut = DateTime.UtcNow.AddDays(4),
            Adults = 2,
            Children = 0
        };
    }
}