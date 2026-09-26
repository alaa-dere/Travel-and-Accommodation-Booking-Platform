using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Rooms.Dtos;
using HotelBooking.Application.Rooms.Update;
using HotelBooking.Domain.Entities;
using Moq;

namespace HotelBooking.UnitTests.Rooms.Update;

public class UpdateRoomTests
{
    private readonly Mock<IRoomRepository> _roomRepositoryMock;
    private readonly Mock<IHotelRepository> _hotelRepositoryMock;
    private readonly UpdateRoom _service;

    public UpdateRoomTests()
    {
        _roomRepositoryMock = new Mock<IRoomRepository>();
        _hotelRepositoryMock = new Mock<IHotelRepository>();

        _service = new UpdateRoom(
            _roomRepositoryMock.Object,
            _hotelRepositoryMock.Object);
    }

    [Fact]
    public async Task UpdateRoomAsync_WhenRoomDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        const int roomId = 10;
        var request = CreateValidRequest();

        _roomRepositoryMock
            .Setup(repository => repository.GetRoomByIdAsync(roomId))
            .ReturnsAsync((Room?)null);

        // Act
        var action = async () =>
            await _service.UpdateRoomAsync(roomId, request);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);

        _hotelRepositoryMock.Verify(
            repository => repository.GetHotelByIdAsync(It.IsAny<int>()),
            Times.Never);

        _roomRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task UpdateRoomAsync_WhenHotelDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        const int roomId = 10;
        var request = CreateValidRequest();
        var room = CreateRoom(roomId);

        _roomRepositoryMock
            .Setup(repository => repository.GetRoomByIdAsync(roomId))
            .ReturnsAsync(room);

        _hotelRepositoryMock
            .Setup(repository => repository.GetHotelByIdAsync(request.HotelId))
            .ReturnsAsync((Hotel?)null);

        // Act
        var action = async () =>
            await _service.UpdateRoomAsync(roomId, request);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);

        _roomRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task UpdateRoomAsync_ShouldRequestCorrectRoom()
    {
        // Arrange
        const int roomId = 25;
        var request = CreateValidRequest();
        var room = CreateRoom(roomId);
        var hotel = CreateHotel(request.HotelId);

        SetupRoomAndHotel(roomId, request.HotelId, room, hotel);

        // Act
        await _service.UpdateRoomAsync(roomId, request);

        // Assert
        _roomRepositoryMock.Verify(
            repository => repository.GetRoomByIdAsync(roomId),
            Times.Once);
    }

    [Fact]
    public async Task UpdateRoomAsync_ShouldRequestCorrectHotel()
    {
        // Arrange
        const int roomId = 10;
        var request = CreateValidRequest();
        var room = CreateRoom(roomId);
        var hotel = CreateHotel(request.HotelId);

        SetupRoomAndHotel(roomId, request.HotelId, room, hotel);

        // Act
        await _service.UpdateRoomAsync(roomId, request);

        // Assert
        _hotelRepositoryMock.Verify(
            repository => repository.GetHotelByIdAsync(request.HotelId),
            Times.Once);
    }

    [Fact]
    public async Task UpdateRoomAsync_WhenRequestIsValid_ShouldUpdateRoomFields()
    {
        // Arrange
        const int roomId = 10;

        var request = CreateValidRequest();

        var room = CreateRoom(roomId);
        var hotel = CreateHotel(request.HotelId);

        SetupRoomAndHotel(roomId, request.HotelId, room, hotel);

        // Act
        await _service.UpdateRoomAsync(roomId, request);

        // Assert
        Assert.Equal(request.RoomNumber, room.RoomNumber);
        Assert.Equal(request.RoomType, room.RoomType);
        Assert.Equal(request.PricePerNight, room.PricePerNight);
        Assert.Equal(request.AdultsCapacity, room.AdultsCapacity);
        Assert.Equal(request.ChildCapacity, room.ChildCapacity);
        Assert.Equal(request.HotelId, room.HotelId);
        Assert.Equal(request.Description, room.Description);
    }

    [Fact]
    public async Task UpdateRoomAsync_WhenRequestIsValid_ShouldPreserveRoomStatuses()
    {
        // Arrange
        const int roomId = 10;

        var request = CreateValidRequest();
        var room = CreateRoom(roomId);

        room.ChangeStatus(false);
        room.ChangeOperationalAvailability(false);

        var hotel = CreateHotel(request.HotelId);

        SetupRoomAndHotel(roomId, request.HotelId, room, hotel);

        // Act
        await _service.UpdateRoomAsync(roomId, request);

        // Assert
        Assert.False(room.IsActive);
        Assert.False(room.IsOperationallyAvailable);
    }

    [Fact]
    public async Task UpdateRoomAsync_WhenRequestIsValid_ShouldSaveChangesOnce()
    {
        // Arrange
        const int roomId = 10;

        var request = CreateValidRequest();
        var room = CreateRoom(roomId);
        var hotel = CreateHotel(request.HotelId);

        SetupRoomAndHotel(roomId, request.HotelId, room, hotel);

        // Act
        await _service.UpdateRoomAsync(roomId, request);

        // Assert
        _roomRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task UpdateRoomAsync_WhenRequestIsValid_ShouldReturnCorrectResponse()
    {
        // Arrange
        const int roomId = 10;

        var request = CreateValidRequest();
        var room = CreateRoom(roomId);
        var hotel = CreateHotel(request.HotelId);

        SetupRoomAndHotel(roomId, request.HotelId, room, hotel);

        // Act
        var result = await _service.UpdateRoomAsync(roomId, request);

        // Assert
        Assert.Equal(roomId, result.RoomId);
        Assert.Equal(request.HotelId, result.HotelId);
        Assert.Equal(request.RoomNumber, result.RoomNumber);
        Assert.Equal(request.RoomType, result.RoomType);
        Assert.Equal(request.AdultsCapacity, result.AdultsCapacity);
        Assert.Equal(request.ChildCapacity, result.ChildCapacity);
        Assert.Equal(request.PricePerNight, result.PricePerNight);
        Assert.Equal(room.IsOperationallyAvailable, result.IsOperationallyAvailable);
        Assert.Equal(room.IsActive, result.IsActive);
        Assert.Equal(request.Description, result.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateRoomAsync_WhenRoomNumberIsInvalid_ShouldThrowArgumentException(
        string roomNumber)
    {
        // Arrange
        const int roomId = 10;

        var request = CreateValidRequest();
        request.RoomNumber = roomNumber;

        var room = CreateRoom(roomId);
        var hotel = CreateHotel(request.HotelId);

        SetupRoomAndHotel(roomId, request.HotelId, room, hotel);

        // Act
        var action = async () =>
            await _service.UpdateRoomAsync(roomId, request);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);

        VerifyChangesWereNotSaved();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateRoomAsync_WhenPricePerNightIsInvalid_ShouldThrowArgumentException(
        int price)
    {
        // Arrange
        const int roomId = 10;

        var request = CreateValidRequest();
        request.PricePerNight = price;

        var room = CreateRoom(roomId);
        var hotel = CreateHotel(request.HotelId);

        SetupRoomAndHotel(roomId, request.HotelId, room, hotel);

        // Act
        var action = async () =>
            await _service.UpdateRoomAsync(roomId, request);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);

        VerifyChangesWereNotSaved();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateRoomAsync_WhenAdultsCapacityIsInvalid_ShouldThrowArgumentException(
        int adultsCapacity)
    {
        // Arrange
        const int roomId = 10;

        var request = CreateValidRequest();
        request.AdultsCapacity = adultsCapacity;

        var room = CreateRoom(roomId);
        var hotel = CreateHotel(request.HotelId);

        SetupRoomAndHotel(roomId, request.HotelId, room, hotel);

        // Act
        var action = async () =>
            await _service.UpdateRoomAsync(roomId, request);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);

        VerifyChangesWereNotSaved();
    }

    [Fact]
    public async Task UpdateRoomAsync_WhenChildCapacityIsNegative_ShouldThrowArgumentException()
    {
        // Arrange
        const int roomId = 10;

        var request = CreateValidRequest();
        request.ChildCapacity = -1;

        var room = CreateRoom(roomId);
        var hotel = CreateHotel(request.HotelId);

        SetupRoomAndHotel(roomId, request.HotelId, room, hotel);

        // Act
        var action = async () =>
            await _service.UpdateRoomAsync(roomId, request);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);

        VerifyChangesWereNotSaved();
    }

    private void SetupRoomAndHotel(
        int roomId,
        int hotelId,
        Room room,
        Hotel hotel)
    {
        _roomRepositoryMock
            .Setup(repository => repository.GetRoomByIdAsync(roomId))
            .ReturnsAsync(room);

        _hotelRepositoryMock
            .Setup(repository => repository.GetHotelByIdAsync(hotelId))
            .ReturnsAsync(hotel);
    }

    private void VerifyChangesWereNotSaved()
    {
        _roomRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    private static RoomRequestDto CreateValidRequest()
    {
        return new RoomRequestDto
        {
            HotelId = 20,
            RoomNumber = "205",
            RoomType = (RoomType)1,
            PricePerNight = 200m,
            AdultsCapacity = 3,
            ChildCapacity = 2,
            Description = "Updated room"
        };
    }

    private static Room CreateRoom(int roomId)
    {
        return new Room(
            roomNumber: "101",
            roomType: (RoomType)1,
            pricePerNight: 100m,
            adultsCapacity: 2,
            childCapacity: 1,
            hotelId: 10,
            description: "Old room")
        {
            RoomId = roomId
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