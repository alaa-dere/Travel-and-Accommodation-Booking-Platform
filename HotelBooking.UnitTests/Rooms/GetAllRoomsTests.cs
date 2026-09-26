using HotelBooking.Application.AvailableRooms.Dtos;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Rooms.Dtos;
using HotelBooking.Application.Rooms.Retrive;
using HotelBooking.Domain.Entities;
using Moq;

namespace HotelBooking.UnitTests.Rooms.Retrieve;

public class GetAllRoomsTests
{
    private readonly Mock<IRoomRepository> _roomRepositoryMock;
    private readonly GetAllRooms _service;

    public GetAllRoomsTests()
    {
        _roomRepositoryMock = new Mock<IRoomRepository>();

        _service = new GetAllRooms(
            _roomRepositoryMock.Object);
    }

    [Fact]
    public async Task GetAllRoomsAsync_WhenNoRoomsExist_ShouldReturnEmptyCollection()
    {
        // Arrange
        var filter = new RoomFilterDto();

        _roomRepositoryMock
            .Setup(repository => repository.GetRoomsAsync(filter))
            .ReturnsAsync(new List<Room>());

        // Act
        var result = await _service.GetAllRoomsAsync(filter);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllRoomsAsync_ShouldPassCorrectFilterToRepository()
    {
        // Arrange
        var filter = new RoomFilterDto();

        _roomRepositoryMock
            .Setup(repository => repository.GetRoomsAsync(filter))
            .ReturnsAsync(new List<Room>());

        // Act
        await _service.GetAllRoomsAsync(filter);

        // Assert
        _roomRepositoryMock.Verify(
            repository => repository.GetRoomsAsync(filter),
            Times.Once);
    }

    [Fact]
    public async Task GetAllRoomsAsync_ShouldMapRoomCorrectly()
    {
        // Arrange
        var filter = new RoomFilterDto();

        var room = CreateRoom(10);

        _roomRepositoryMock
            .Setup(repository => repository.GetRoomsAsync(filter))
            .ReturnsAsync(new List<Room> { room });

        // Act
        var result = (await _service.GetAllRoomsAsync(filter)).ToList();

        // Assert
        Assert.Single(result);

        var response = result[0];

        Assert.Equal(room.RoomId, response.RoomId);
        Assert.Equal(room.HotelId, response.HotelId);
        Assert.Equal(room.RoomNumber, response.RoomNumber);
        Assert.Equal(room.RoomType, response.RoomType);
        Assert.Equal(room.AdultsCapacity, response.AdultsCapacity);
        Assert.Equal(room.ChildCapacity, response.ChildCapacity);
        Assert.Equal(room.PricePerNight, response.PricePerNight);
        Assert.Equal(
            room.IsOperationallyAvailable,
            response.IsOperationallyAvailable);
        Assert.Equal(room.IsActive, response.IsActive);
        Assert.Equal(room.Description, response.Description);
    }

    [Fact]
    public async Task GetAllRoomsAsync_WhenMultipleRoomsExist_ShouldReturnAllRooms()
    {
        // Arrange
        var filter = new RoomFilterDto();

        var rooms = new List<Room>
        {
            CreateRoom(1, "101"),
            CreateRoom(2, "102"),
            CreateRoom(3, "103")
        };

        _roomRepositoryMock
            .Setup(repository => repository.GetRoomsAsync(filter))
            .ReturnsAsync(rooms);

        // Act
        var result = (await _service.GetAllRoomsAsync(filter)).ToList();

        // Assert
        Assert.Equal(3, result.Count);

        Assert.Contains(result, room => room.RoomId == 1);
        Assert.Contains(result, room => room.RoomId == 2);
        Assert.Contains(result, room => room.RoomId == 3);
    }

    [Fact]
    public async Task GetAllRoomsAsync_WhenRoomHasNoImages_ShouldReturnEmptyImages()
    {
        // Arrange
        var filter = new RoomFilterDto();

        var room = CreateRoom(10);

        _roomRepositoryMock
            .Setup(repository => repository.GetRoomsAsync(filter))
            .ReturnsAsync(new List<Room> { room });

        // Act
        var result = (await _service.GetAllRoomsAsync(filter)).Single();

        // Assert
        Assert.Empty(result.Images);
    }

    [Fact]
    public async Task GetAllRoomsAsync_ShouldMapRoomImagesCorrectly()
    {
        // Arrange
        var filter = new RoomFilterDto();

        var room = CreateRoom(10);

        room.RoomImages.Add(
            new RoomImage(
                "https://example.com/first.jpg",
                1,
                room.RoomId));

        room.RoomImages.Add(
            new RoomImage(
                "https://example.com/second.jpg",
                2,
                room.RoomId));

        _roomRepositoryMock
            .Setup(repository => repository.GetRoomsAsync(filter))
            .ReturnsAsync(new List<Room> { room });

        // Act
        var result = (await _service.GetAllRoomsAsync(filter)).Single();

        // Assert
        Assert.Equal(2, result.Images.Count);

        Assert.Equal(
            "https://example.com/first.jpg",
            result.Images[0].ImageUrl);

        Assert.Equal(1, result.Images[0].DisplayOrder);

        Assert.Equal(
            "https://example.com/second.jpg",
            result.Images[1].ImageUrl);

        Assert.Equal(2, result.Images[1].DisplayOrder);
    }

    [Fact]
    public async Task GetAllRoomsAsync_ShouldOrderImagesByDisplayOrder()
    {
        // Arrange
        var filter = new RoomFilterDto();

        var room = CreateRoom(10);

        room.RoomImages.Add(
            new RoomImage(
                "https://example.com/third.jpg",
                3,
                room.RoomId));

        room.RoomImages.Add(
            new RoomImage(
                "https://example.com/first.jpg",
                1,
                room.RoomId));

        room.RoomImages.Add(
            new RoomImage(
                "https://example.com/second.jpg",
                2,
                room.RoomId));

        _roomRepositoryMock
            .Setup(repository => repository.GetRoomsAsync(filter))
            .ReturnsAsync(new List<Room> { room });

        // Act
        var result = (await _service.GetAllRoomsAsync(filter)).Single();

        // Assert
        Assert.Equal(3, result.Images.Count);

        Assert.Equal(1, result.Images[0].DisplayOrder);
        Assert.Equal(2, result.Images[1].DisplayOrder);
        Assert.Equal(3, result.Images[2].DisplayOrder);

        Assert.Equal(
            "https://example.com/first.jpg",
            result.Images[0].ImageUrl);

        Assert.Equal(
            "https://example.com/second.jpg",
            result.Images[1].ImageUrl);

        Assert.Equal(
            "https://example.com/third.jpg",
            result.Images[2].ImageUrl);
    }

    [Fact]
    public async Task GetAllRoomsAsync_ShouldPreserveRoomStatuses()
    {
        // Arrange
        var filter = new RoomFilterDto();

        var room = CreateRoom(10);

        room.ChangeStatus(false);
        room.ChangeOperationalAvailability(false);

        _roomRepositoryMock
            .Setup(repository => repository.GetRoomsAsync(filter))
            .ReturnsAsync(new List<Room> { room });

        // Act
        var result = (await _service.GetAllRoomsAsync(filter)).Single();

        // Assert
        Assert.False(result.IsActive);
        Assert.False(result.IsOperationallyAvailable);
    }

    private static Room CreateRoom(
        int roomId,
        string roomNumber = "101")
    {
        return new Room(
            roomNumber: roomNumber,
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