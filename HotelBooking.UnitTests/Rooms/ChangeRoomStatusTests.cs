using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Rooms.Delete;
using HotelBooking.Domain.Entities;
using Moq;

namespace HotelBooking.UnitTests.Rooms.Delete;

public class ChangeRoomStatusTests
{
    private readonly Mock<IRoomRepository> _roomRepositoryMock;
    private readonly ChangeRoomStatus _service;

    public ChangeRoomStatusTests()
    {
        _roomRepositoryMock = new Mock<IRoomRepository>();

        _service = new ChangeRoomStatus(
            _roomRepositoryMock.Object);
    }

    [Fact]
    public async Task ChangeRoomStatusAsync_WhenRoomDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        const int roomId = 10;

        _roomRepositoryMock
            .Setup(repository =>
                repository.GetRoomByIdAsync(roomId))
            .ReturnsAsync((Room?)null);

        // Act
        var action = async () =>
            await _service.ChangeRoomStatusAsync(
                roomId,
                true);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);
    }

    [Fact]
    public async Task ChangeRoomStatusAsync_WhenRoomDoesNotExist_ShouldNotSaveChanges()
    {
        // Arrange
        const int roomId = 10;

        _roomRepositoryMock
            .Setup(repository =>
                repository.GetRoomByIdAsync(roomId))
            .ReturnsAsync((Room?)null);

        // Act
        try
        {
            await _service.ChangeRoomStatusAsync(
                roomId,
                true);
        }
        catch (NotFoundException)
        {
        }

        // Assert
        _roomRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task ChangeRoomStatusAsync_WhenIsActiveIsTrue_ShouldActivateRoom()
    {
        // Arrange
        const int roomId = 10;

        var room = CreateRoom(roomId);
        room.ChangeStatus(false);

        _roomRepositoryMock
            .Setup(repository =>
                repository.GetRoomByIdAsync(roomId))
            .ReturnsAsync(room);

        // Act
        await _service.ChangeRoomStatusAsync(
            roomId,
            true);

        // Assert
        Assert.True(room.IsActive);
    }

    [Fact]
    public async Task ChangeRoomStatusAsync_WhenIsActiveIsFalse_ShouldDeactivateRoom()
    {
        // Arrange
        const int roomId = 10;

        var room = CreateRoom(roomId);
        room.ChangeStatus(true);

        _roomRepositoryMock
            .Setup(repository =>
                repository.GetRoomByIdAsync(roomId))
            .ReturnsAsync(room);

        // Act
        await _service.ChangeRoomStatusAsync(
            roomId,
            false);

        // Assert
        Assert.False(room.IsActive);
    }

    [Fact]
    public async Task ChangeRoomStatusAsync_WhenRoomExists_ShouldSaveChangesOnce()
    {
        // Arrange
        const int roomId = 10;

        var room = CreateRoom(roomId);

        _roomRepositoryMock
            .Setup(repository =>
                repository.GetRoomByIdAsync(roomId))
            .ReturnsAsync(room);

        // Act
        await _service.ChangeRoomStatusAsync(
            roomId,
            false);

        // Assert
        _roomRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task ChangeRoomStatusAsync_ShouldRequestCorrectRoom()
    {
        // Arrange
        const int roomId = 25;

        var room = CreateRoom(roomId);

        _roomRepositoryMock
            .Setup(repository =>
                repository.GetRoomByIdAsync(roomId))
            .ReturnsAsync(room);

        // Act
        await _service.ChangeRoomStatusAsync(
            roomId,
            true);

        // Assert
        _roomRepositoryMock.Verify(
            repository => repository.GetRoomByIdAsync(roomId),
            Times.Once);
    }

    private static Room CreateRoom(int roomId)
    {
        return new Room(
            roomNumber: "101",
            roomType: (RoomType)1,
            pricePerNight: 150m,
            adultsCapacity: 2,
            childCapacity: 1,
            hotelId: 10,
            description: "Test room")
        {
            RoomId = roomId
        };
    }
}