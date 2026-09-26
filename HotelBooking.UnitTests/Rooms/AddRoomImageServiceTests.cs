using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Rooms.Dtos;
using HotelBooking.Application.Rooms.Images;
using HotelBooking.Domain.Entities;
using Moq;

namespace HotelBooking.UnitTests.Rooms.Images;

public class AddRoomImageServiceTests
{
    private readonly Mock<IRoomRepository> _roomRepositoryMock;
    private readonly Mock<IRoomImageRepository> _roomImageRepositoryMock;
    private readonly AddRoomImageService _service;

    public AddRoomImageServiceTests()
    {
        _roomRepositoryMock = new Mock<IRoomRepository>();
        _roomImageRepositoryMock = new Mock<IRoomImageRepository>();

        _service = new AddRoomImageService(
            _roomRepositoryMock.Object,
            _roomImageRepositoryMock.Object);
    }

    [Fact]
    public async Task AddRoomImageAsync_WhenRoomDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        const int roomId = 10;

        var request = CreateValidRequest();

        _roomRepositoryMock
            .Setup(repository =>
                repository.GetRoomByIdAsync(roomId))
            .ReturnsAsync((Room?)null);

        // Act
        var action = async () =>
            await _service.AddRoomImageAsync(roomId, request);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);
    }

    [Fact]
    public async Task AddRoomImageAsync_WhenRoomDoesNotExist_ShouldNotAddOrSaveImage()
    {
        // Arrange
        const int roomId = 10;

        var request = CreateValidRequest();

        _roomRepositoryMock
            .Setup(repository =>
                repository.GetRoomByIdAsync(roomId))
            .ReturnsAsync((Room?)null);

        // Act
        try
        {
            await _service.AddRoomImageAsync(roomId, request);
        }
        catch (NotFoundException)
        {
        }

        // Assert
        _roomImageRepositoryMock.Verify(
            repository =>
                repository.AddAsync(It.IsAny<RoomImage>()),
            Times.Never);

        _roomImageRepositoryMock.Verify(
            repository =>
                repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task AddRoomImageAsync_ShouldRequestCorrectRoom()
    {
        // Arrange
        const int roomId = 25;

        var request = CreateValidRequest();
        var room = CreateRoom(roomId);

        _roomRepositoryMock
            .Setup(repository =>
                repository.GetRoomByIdAsync(roomId))
            .ReturnsAsync(room);

        // Act
        await _service.AddRoomImageAsync(roomId, request);

        // Assert
        _roomRepositoryMock.Verify(
            repository =>
                repository.GetRoomByIdAsync(roomId),
            Times.Once);
    }

    [Fact]
    public async Task AddRoomImageAsync_WhenRoomExists_ShouldCreateImageWithCorrectData()
    {
        // Arrange
        const int roomId = 10;

        var request = CreateValidRequest();
        var room = CreateRoom(roomId);

        _roomRepositoryMock
            .Setup(repository =>
                repository.GetRoomByIdAsync(roomId))
            .ReturnsAsync(room);

        RoomImage? addedImage = null;

        _roomImageRepositoryMock
            .Setup(repository =>
                repository.AddAsync(It.IsAny<RoomImage>()))
            .Callback<RoomImage>(image =>
                addedImage = image);

        // Act
        await _service.AddRoomImageAsync(roomId, request);

        // Assert
        Assert.NotNull(addedImage);
        Assert.Equal(roomId, addedImage!.RoomId);
        Assert.Equal(request.ImageUrl, addedImage.ImageUrl);
        Assert.Equal(request.DisplayOrder, addedImage.DisplayOrder);
    }

    [Fact]
    public async Task AddRoomImageAsync_WhenRoomExists_ShouldAddImageOnce()
    {
        // Arrange
        const int roomId = 10;

        var request = CreateValidRequest();
        var room = CreateRoom(roomId);

        _roomRepositoryMock
            .Setup(repository =>
                repository.GetRoomByIdAsync(roomId))
            .ReturnsAsync(room);

        // Act
        await _service.AddRoomImageAsync(roomId, request);

        // Assert
        _roomImageRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.Is<RoomImage>(image =>
                    image.RoomId == roomId &&
                    image.ImageUrl == request.ImageUrl &&
                    image.DisplayOrder == request.DisplayOrder)),
            Times.Once);
    }

    [Fact]
    public async Task AddRoomImageAsync_WhenRoomExists_ShouldSaveChangesOnce()
    {
        // Arrange
        const int roomId = 10;

        var request = CreateValidRequest();
        var room = CreateRoom(roomId);

        _roomRepositoryMock
            .Setup(repository =>
                repository.GetRoomByIdAsync(roomId))
            .ReturnsAsync(room);

        // Act
        await _service.AddRoomImageAsync(roomId, request);

        // Assert
        _roomImageRepositoryMock.Verify(
            repository =>
                repository.SaveChangesAsync(),
            Times.Once);
    }

    private static AddRoomImageRequestDto CreateValidRequest()
    {
        return new AddRoomImageRequestDto
        {
            ImageUrl = "https://example.com/room.jpg",
            DisplayOrder = 1
        };
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