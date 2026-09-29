using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Rooms.Update;
using HotelBooking.Domain.Entities;
using Moq;

namespace HotelBooking.UnitTests.Rooms.Update;

public class ChangeRoomOperationalAvailabilityTests
{
    private readonly Mock<IRoomRepository> _roomRepositoryMock;
    private readonly ChangeRoomOperationalAvailability _service;

    public ChangeRoomOperationalAvailabilityTests()
    {
        _roomRepositoryMock = new Mock<IRoomRepository>();

        _service = new ChangeRoomOperationalAvailability(
            _roomRepositoryMock.Object);
    }

    [Fact]
    public async Task ChangeOperationalAvailabilityAsync_WhenRoomDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        const int roomId = 10;

        _roomRepositoryMock
            .Setup(repository =>
                repository.GetRoomByIdAsync(roomId))
            .ReturnsAsync((Room?)null);

        // Act
        var action = async () =>
            await _service.ChangeOperationalAvailabilityAsync(
                roomId,
                true);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);
    }

    [Fact]
    public async Task ChangeOperationalAvailabilityAsync_WhenRoomDoesNotExist_ShouldNotSaveChanges()
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
            await _service.ChangeOperationalAvailabilityAsync(
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
    public async Task ChangeOperationalAvailabilityAsync_WhenTrue_ShouldMakeRoomOperationallyAvailable()
    {
        // Arrange
        const int roomId = 10;

        var room = CreateRoom(roomId);
        room.ChangeOperationalAvailability(false);

        _roomRepositoryMock
            .Setup(repository =>
                repository.GetRoomByIdAsync(roomId))
            .ReturnsAsync(room);

        // Act
        await _service.ChangeOperationalAvailabilityAsync(
            roomId,
            true);

        // Assert
        Assert.True(room.IsOperationallyAvailable);
    }

    [Fact]
    public async Task ChangeOperationalAvailabilityAsync_WhenFalse_ShouldMakeRoomOperationallyUnavailable()
    {
        // Arrange
        const int roomId = 10;

        var room = CreateRoom(roomId);
        room.ChangeOperationalAvailability(true);

        _roomRepositoryMock
            .Setup(repository =>
                repository.GetRoomByIdAsync(roomId))
            .ReturnsAsync(room);

        // Act
        await _service.ChangeOperationalAvailabilityAsync(
            roomId,
            false);

        // Assert
        Assert.False(room.IsOperationallyAvailable);
    }

    [Fact]
    public async Task ChangeOperationalAvailabilityAsync_WhenRoomExists_ShouldSaveChangesOnce()
    {
        // Arrange
        const int roomId = 10;

        var room = CreateRoom(roomId);

        _roomRepositoryMock
            .Setup(repository =>
                repository.GetRoomByIdAsync(roomId))
            .ReturnsAsync(room);

        // Act
        await _service.ChangeOperationalAvailabilityAsync(
            roomId,
            false);

        // Assert
        _roomRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task ChangeOperationalAvailabilityAsync_ShouldRequestCorrectRoom()
    {
        // Arrange
        const int roomId = 25;

        var room = CreateRoom(roomId);

        _roomRepositoryMock
            .Setup(repository =>
                repository.GetRoomByIdAsync(roomId))
            .ReturnsAsync(room);

        // Act
        await _service.ChangeOperationalAvailabilityAsync(
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