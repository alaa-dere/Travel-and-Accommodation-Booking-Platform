using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Rooms.Create;
using HotelBooking.Application.Rooms.Dtos;
using HotelBooking.Domain.Entities;
using Moq;

namespace HotelBooking.UnitTests.Rooms.Create;

public class CreateRoomTests
{
    private readonly Mock<IRoomRepository> _roomRepositoryMock;
    private readonly Mock<IHotelRepository> _hotelRepositoryMock;
    private readonly CreateRoom _service;

    public CreateRoomTests()
    {
        _roomRepositoryMock = new Mock<IRoomRepository>();
        _hotelRepositoryMock = new Mock<IHotelRepository>();

        _service = new CreateRoom(
            _roomRepositoryMock.Object,
            _hotelRepositoryMock.Object);
    }

    [Fact]
    public async Task CreateRoomAsync_WhenHotelDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        var request = CreateValidRequest();

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(request.HotelId))
            .ReturnsAsync((Hotel?)null);

        // Act
        var action = async () =>
            await _service.CreateRoomAsync(request);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);

        _roomRepositoryMock.Verify(
            repository => repository.Add(It.IsAny<Room>()),
            Times.Never);

        _roomRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task CreateRoomAsync_ShouldCheckCorrectHotel()
    {
        // Arrange
        var request = CreateValidRequest();
        var hotel = CreateHotel(request.HotelId);

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(request.HotelId))
            .ReturnsAsync(hotel);

        // Act
        await _service.CreateRoomAsync(request);

        // Assert
        _hotelRepositoryMock.Verify(
            repository =>
                repository.GetHotelByIdAsync(request.HotelId),
            Times.Once);
    }

    [Fact]
    public async Task CreateRoomAsync_WhenRequestIsValid_ShouldAddRoomWithCorrectData()
    {
        // Arrange
        var request = CreateValidRequest();
        var hotel = CreateHotel(request.HotelId);

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(request.HotelId))
            .ReturnsAsync(hotel);

        Room? addedRoom = null;

        _roomRepositoryMock
            .Setup(repository =>
                repository.Add(It.IsAny<Room>()))
            .Callback<Room>(room =>
                addedRoom = room);

        // Act
        await _service.CreateRoomAsync(request);

        // Assert
        Assert.NotNull(addedRoom);

        Assert.Equal(request.HotelId, addedRoom!.HotelId);
        Assert.Equal(request.RoomNumber, addedRoom.RoomNumber);
        Assert.Equal(request.RoomType, addedRoom.RoomType);
        Assert.Equal(request.PricePerNight, addedRoom.PricePerNight);
        Assert.Equal(request.AdultsCapacity, addedRoom.AdultsCapacity);
        Assert.Equal(request.ChildCapacity, addedRoom.ChildCapacity);
        Assert.Equal(request.Description, addedRoom.Description);
    }

    [Fact]
    public async Task CreateRoomAsync_WhenRequestIsValid_ShouldAddRoomOnce()
    {
        // Arrange
        var request = CreateValidRequest();
        var hotel = CreateHotel(request.HotelId);

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(request.HotelId))
            .ReturnsAsync(hotel);

        // Act
        await _service.CreateRoomAsync(request);

        // Assert
        _roomRepositoryMock.Verify(
            repository => repository.Add(
                It.Is<Room>(room =>
                    room.HotelId == request.HotelId &&
                    room.RoomNumber == request.RoomNumber &&
                    room.RoomType == request.RoomType &&
                    room.PricePerNight == request.PricePerNight &&
                    room.AdultsCapacity == request.AdultsCapacity &&
                    room.ChildCapacity == request.ChildCapacity &&
                    room.Description == request.Description)),
            Times.Once);
    }

    [Fact]
    public async Task CreateRoomAsync_WhenRequestIsValid_ShouldSaveChangesOnce()
    {
        // Arrange
        var request = CreateValidRequest();
        var hotel = CreateHotel(request.HotelId);

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(request.HotelId))
            .ReturnsAsync(hotel);

        // Act
        await _service.CreateRoomAsync(request);

        // Assert
        _roomRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task CreateRoomAsync_WhenRequestIsValid_ShouldReturnCorrectResponse()
    {
        // Arrange
        var request = CreateValidRequest();
        var hotel = CreateHotel(request.HotelId);

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(request.HotelId))
            .ReturnsAsync(hotel);

        _roomRepositoryMock
            .Setup(repository =>
                repository.Add(It.IsAny<Room>()))
            .Callback<Room>(room =>
                room.RoomId = 100);

        // Act
        var result = await _service.CreateRoomAsync(request);

        // Assert
        Assert.Equal(100, result.RoomId);
        Assert.Equal(request.HotelId, result.HotelId);
        Assert.Equal(request.RoomNumber, result.RoomNumber);
        Assert.Equal(request.RoomType, result.RoomType);
        Assert.Equal(request.AdultsCapacity, result.AdultsCapacity);
        Assert.Equal(request.ChildCapacity, result.ChildCapacity);
        Assert.Equal(request.PricePerNight, result.PricePerNight);
        Assert.Equal(request.Description, result.Description);
    }

    [Fact]
    public async Task CreateRoomAsync_ShouldMapRoomStatusToResponse()
    {
        // Arrange
        var request = CreateValidRequest();
        var hotel = CreateHotel(request.HotelId);

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(request.HotelId))
            .ReturnsAsync(hotel);

        Room? addedRoom = null;

        _roomRepositoryMock
            .Setup(repository =>
                repository.Add(It.IsAny<Room>()))
            .Callback<Room>(room =>
                addedRoom = room);

        // Act
        var result = await _service.CreateRoomAsync(request);

        // Assert
        Assert.NotNull(addedRoom);
        Assert.Equal(addedRoom!.IsActive, result.IsActive);
        Assert.Equal(
            addedRoom.IsOperationallyAvailable,
            result.IsOperationallyAvailable);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateRoomAsync_WhenRoomNumberIsInvalid_ShouldThrowArgumentException(
        string roomNumber)
    {
        // Arrange
        var request = CreateValidRequest();
        request.RoomNumber = roomNumber;

        var hotel = CreateHotel(request.HotelId);

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(request.HotelId))
            .ReturnsAsync(hotel);

        // Act
        var action = async () =>
            await _service.CreateRoomAsync(request);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);

        VerifyRoomWasNotSaved();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateRoomAsync_WhenPricePerNightIsInvalid_ShouldThrowArgumentException(
        int price)
    {
        // Arrange
        var request = CreateValidRequest();
        request.PricePerNight = price;

        var hotel = CreateHotel(request.HotelId);

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(request.HotelId))
            .ReturnsAsync(hotel);

        // Act
        var action = async () =>
            await _service.CreateRoomAsync(request);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);

        VerifyRoomWasNotSaved();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateRoomAsync_WhenAdultsCapacityIsInvalid_ShouldThrowArgumentException(
        int adultsCapacity)
    {
        // Arrange
        var request = CreateValidRequest();
        request.AdultsCapacity = adultsCapacity;

        var hotel = CreateHotel(request.HotelId);

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(request.HotelId))
            .ReturnsAsync(hotel);

        // Act
        var action = async () =>
            await _service.CreateRoomAsync(request);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);

        VerifyRoomWasNotSaved();
    }

    [Fact]
    public async Task CreateRoomAsync_WhenChildCapacityIsNegative_ShouldThrowArgumentException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.ChildCapacity = -1;

        var hotel = CreateHotel(request.HotelId);

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(request.HotelId))
            .ReturnsAsync(hotel);

        // Act
        var action = async () =>
            await _service.CreateRoomAsync(request);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);

        VerifyRoomWasNotSaved();
    }

    private void VerifyRoomWasNotSaved()
    {
        _roomRepositoryMock.Verify(
            repository => repository.Add(It.IsAny<Room>()),
            Times.Never);

        _roomRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    private static RoomRequestDto CreateValidRequest()
    {
        return new RoomRequestDto
        {
            HotelId = 10,
            RoomNumber = "101",
            RoomType = (RoomType)1,
            PricePerNight = 150m,
            AdultsCapacity = 2,
            ChildCapacity = 1,
            Description = "Test room"
        };
    }

    private static Hotel CreateHotel(int hotelId)
    {
        return new Hotel(
            name: "Test Hotel",
            ownerName: "Test Owner",
            address: "Test Address",
            latitude: 32.2211,
            longitude: 35.2544,
            hotelType: (HotelType)1,
            cityId: 1,
            description: null,
            history: null)
        {
            HotelId = hotelId
        };
    }
}