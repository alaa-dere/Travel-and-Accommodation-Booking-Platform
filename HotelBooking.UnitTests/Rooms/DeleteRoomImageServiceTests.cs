using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Rooms.Images;
using HotelBooking.Domain.Entities;
using Moq;

namespace HotelBooking.UnitTests.Rooms.Images;

public class DeleteRoomImageServiceTests
{
    private readonly Mock<IRoomRepository> _roomRepositoryMock;
    private readonly Mock<IRoomImageRepository> _roomImageRepositoryMock;
    private readonly DeleteRoomImageService _service;

    public DeleteRoomImageServiceTests()
    {
        _roomRepositoryMock = new Mock<IRoomRepository>();
        _roomImageRepositoryMock = new Mock<IRoomImageRepository>();

        _service = new DeleteRoomImageService(
            _roomRepositoryMock.Object,
            _roomImageRepositoryMock.Object);
    }

    [Fact]
    public async Task DeleteRoomImageAsync_WhenRoomDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        const int roomId = 10;
        const int imageId = 20;

        _roomRepositoryMock
            .Setup(repository => repository.GetRoomByIdAsync(roomId))
            .ReturnsAsync((Room?)null);

        // Act
        var action = async () =>
            await _service.DeleteRoomImageAsync(roomId, imageId);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);

        _roomImageRepositoryMock.Verify(
            repository => repository.GetByIdAsync(It.IsAny<int>()),
            Times.Never);

        _roomImageRepositoryMock.Verify(
            repository => repository.Delete(It.IsAny<RoomImage>()),
            Times.Never);

        _roomImageRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task DeleteRoomImageAsync_WhenImageDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        const int roomId = 10;
        const int imageId = 20;

        var room = CreateRoom(roomId);

        _roomRepositoryMock
            .Setup(repository => repository.GetRoomByIdAsync(roomId))
            .ReturnsAsync(room);

        _roomImageRepositoryMock
            .Setup(repository => repository.GetByIdAsync(imageId))
            .ReturnsAsync((RoomImage?)null);

        // Act
        var action = async () =>
            await _service.DeleteRoomImageAsync(roomId, imageId);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);

        _roomImageRepositoryMock.Verify(
            repository => repository.Delete(It.IsAny<RoomImage>()),
            Times.Never);

        _roomImageRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task DeleteRoomImageAsync_WhenImageBelongsToDifferentRoom_ShouldThrowNotFoundException()
    {
        // Arrange
        const int roomId = 10;
        const int imageId = 20;

        var room = CreateRoom(roomId);

        var image = new RoomImage(
            "https://example.com/room.jpg",
            1,
            99);

        _roomRepositoryMock
            .Setup(repository => repository.GetRoomByIdAsync(roomId))
            .ReturnsAsync(room);

        _roomImageRepositoryMock
            .Setup(repository => repository.GetByIdAsync(imageId))
            .ReturnsAsync(image);

        // Act
        var action = async () =>
            await _service.DeleteRoomImageAsync(roomId, imageId);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);

        _roomImageRepositoryMock.Verify(
            repository => repository.Delete(It.IsAny<RoomImage>()),
            Times.Never);

        _roomImageRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task DeleteRoomImageAsync_WhenImageBelongsToRoom_ShouldDeleteImage()
    {
        // Arrange
        const int roomId = 10;
        const int imageId = 20;

        var room = CreateRoom(roomId);

        var image = new RoomImage(
            "https://example.com/room.jpg",
            1,
            roomId);

        _roomRepositoryMock
            .Setup(repository => repository.GetRoomByIdAsync(roomId))
            .ReturnsAsync(room);

        _roomImageRepositoryMock
            .Setup(repository => repository.GetByIdAsync(imageId))
            .ReturnsAsync(image);

        // Act
        await _service.DeleteRoomImageAsync(roomId, imageId);

        // Assert
        _roomImageRepositoryMock.Verify(
            repository => repository.Delete(image),
            Times.Once);
    }

    [Fact]
    public async Task DeleteRoomImageAsync_WhenImageBelongsToRoom_ShouldSaveChangesOnce()
    {
        // Arrange
        const int roomId = 10;
        const int imageId = 20;

        var room = CreateRoom(roomId);

        var image = new RoomImage(
            "https://example.com/room.jpg",
            1,
            roomId);

        _roomRepositoryMock
            .Setup(repository => repository.GetRoomByIdAsync(roomId))
            .ReturnsAsync(room);

        _roomImageRepositoryMock
            .Setup(repository => repository.GetByIdAsync(imageId))
            .ReturnsAsync(image);

        // Act
        await _service.DeleteRoomImageAsync(roomId, imageId);

        // Assert
        _roomImageRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task DeleteRoomImageAsync_ShouldRequestCorrectRoomAndImage()
    {
        // Arrange
        const int roomId = 15;
        const int imageId = 25;

        var room = CreateRoom(roomId);

        var image = new RoomImage(
            "https://example.com/room.jpg",
            1,
            roomId);

        _roomRepositoryMock
            .Setup(repository => repository.GetRoomByIdAsync(roomId))
            .ReturnsAsync(room);

        _roomImageRepositoryMock
            .Setup(repository => repository.GetByIdAsync(imageId))
            .ReturnsAsync(image);

        // Act
        await _service.DeleteRoomImageAsync(roomId, imageId);

        // Assert
        _roomRepositoryMock.Verify(
            repository => repository.GetRoomByIdAsync(roomId),
            Times.Once);

        _roomImageRepositoryMock.Verify(
            repository => repository.GetByIdAsync(imageId),
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