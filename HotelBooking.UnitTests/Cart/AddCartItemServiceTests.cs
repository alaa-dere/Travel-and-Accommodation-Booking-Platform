using HotelBooking.Application.AvailableRooms.Dtos;
using HotelBooking.Application.Cart.Create;
using HotelBooking.Application.Cart.Dtos;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using Moq;

namespace HotelBooking.UnitTests.Cart;

public class AddCartItemServiceTests
{
    private readonly Mock<ICartRepository> _cartRepositoryMock;
    private readonly Mock<IAvailableRoomRepository> _availableRoomRepositoryMock;
    private readonly AddCartItemService _service;

    public AddCartItemServiceTests()
    {
        _cartRepositoryMock = new Mock<ICartRepository>();
        _availableRoomRepositoryMock = new Mock<IAvailableRoomRepository>();

        _service = new AddCartItemService(_cartRepositoryMock.Object, _availableRoomRepositoryMock.Object);
    }

    [Fact]
    public async Task AddCartItemAsync_WhenCheckInIsInPast_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.CheckIn = DateTime.UtcNow.Date.AddDays(-1);
        request.CheckOut = DateTime.UtcNow.Date.AddDays(2);

        // Act
        var action = async () => await _service.AddCartItemAsync(1, request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        VerifyAvailableRoomWasNeverRequested();
        VerifyCartWasNeverModified();
    }

    [Fact]
    public async Task AddCartItemAsync_WhenCheckOutIsBeforeCheckIn_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.CheckOut = request.CheckIn.AddDays(-1);

        // Act
        var action = async () => await _service.AddCartItemAsync(1, request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        VerifyAvailableRoomWasNeverRequested();
        VerifyCartWasNeverModified();
    }

    [Fact]
    public async Task AddCartItemAsync_WhenCheckOutEqualsCheckIn_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.CheckOut = request.CheckIn;

        // Act
        var action = async () => await _service.AddCartItemAsync(1, request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        VerifyAvailableRoomWasNeverRequested();
        VerifyCartWasNeverModified();
    }

    [Fact]
    public async Task AddCartItemAsync_WhenAdultsIsZero_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Adults = 0;

        // Act
        var action = async () => await _service.AddCartItemAsync(1, request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        VerifyAvailableRoomWasNeverRequested();
        VerifyCartWasNeverModified();
    }

    [Fact]
    public async Task AddCartItemAsync_WhenAdultsIsNegative_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Adults = -1;

        // Act
        var action = async () => await _service.AddCartItemAsync(1, request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        VerifyAvailableRoomWasNeverRequested();
        VerifyCartWasNeverModified();
    }

    [Fact]
    public async Task AddCartItemAsync_WhenChildrenIsNegative_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Children = -1;

        // Act
        var action = async () => await _service.AddCartItemAsync(1, request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        VerifyAvailableRoomWasNeverRequested();
        VerifyCartWasNeverModified();
    }

    [Fact]
    public async Task AddCartItemAsync_WhenRoomIsNotAvailable_ShouldThrowConflictException()
    {
        // Arrange
        const int userId = 1;
        var request = CreateValidRequest();

        _availableRoomRepositoryMock
            .Setup(repository => repository.GetAvailableRoomAsync(
                request.RoomId,
                request.CheckIn,
                request.CheckOut,
                request.Adults,
                request.Children))
            .ReturnsAsync((AvailableRoomResponseDto?)null);

        // Act
        var action = async () =>
            await _service.AddCartItemAsync(userId, request);

        // Assert
        await Assert.ThrowsAsync<ConflictException>(action);

        _availableRoomRepositoryMock.Verify(
            repository => repository.GetAvailableRoomAsync(
                request.RoomId,
                request.CheckIn,
                request.CheckOut,
                request.Adults,
                request.Children),
            Times.Once);

        VerifyCartWasNeverModified();
    }

    [Fact]
    public async Task AddCartItemAsync_WhenRoomIsAvailable_ShouldAddCartItem()
    {
        // Arrange
        const int userId = 1;
        var request = CreateValidRequest();

        SetupAvailableRoom(request);

        CartItem? addedCartItem = null;

        _cartRepositoryMock
            .Setup(repository => repository.AddAsync(It.IsAny<CartItem>()))
            .Callback<CartItem>(item => addedCartItem = item)
            .Returns(Task.CompletedTask);

        // Act
        await _service.AddCartItemAsync(userId, request);

        // Assert
        Assert.NotNull(addedCartItem);

        Assert.Equal(userId, addedCartItem!.UserId);
        Assert.Equal(request.RoomId, addedCartItem.RoomId);
        Assert.Equal(request.CheckIn, addedCartItem.CheckIn);
        Assert.Equal(request.CheckOut, addedCartItem.CheckOut);
        Assert.Equal(request.Adults, addedCartItem.Adults);
        Assert.Equal(request.Children, addedCartItem.Children);

        _cartRepositoryMock.Verify(repository => repository.AddAsync(It.IsAny<CartItem>()), Times.Once);
    }

    [Fact]
    public async Task AddCartItemAsync_WhenRoomIsAvailable_ShouldSaveChanges()
    {
        // Arrange
        const int userId = 1;
        var request = CreateValidRequest();

        SetupAvailableRoom(request);

        // Act
        await _service.AddCartItemAsync(userId, request);

        // Assert
        _cartRepositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AddCartItemAsync_WhenRoomIsAvailable_ShouldCheckAvailabilityWithRequestedValues()
    {
        // Arrange
        const int userId = 1;
        var request = CreateValidRequest();

        SetupAvailableRoom(request);

        // Act
        await _service.AddCartItemAsync(userId, request);

        // Assert
        _availableRoomRepositoryMock.Verify(
            repository => repository.GetAvailableRoomAsync(
                request.RoomId,
                request.CheckIn,
                request.CheckOut,
                request.Adults,
                request.Children),
            Times.Once);
    }

    private void SetupAvailableRoom(AddCartItemRequestDto request)
    {
        var availableRoom = new AvailableRoomResponseDto
        {
            RoomId = request.RoomId,
            PricePerNight = 100m
        };

        _availableRoomRepositoryMock
            .Setup(repository => repository.GetAvailableRoomAsync(
                request.RoomId,
                request.CheckIn,
                request.CheckOut,
                request.Adults,
                request.Children))
            .ReturnsAsync(availableRoom);
    }

    private void VerifyAvailableRoomWasNeverRequested()
    {
        _availableRoomRepositoryMock.Verify(
            repository => repository.GetAvailableRoomAsync(
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>(),
                It.IsAny<int>()),
            Times.Never);
    }

    private void VerifyCartWasNeverModified()
    {
        _cartRepositoryMock.Verify(repository => repository.AddAsync(It.IsAny<CartItem>()), Times.Never);
        _cartRepositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Never);
    }

    private static AddCartItemRequestDto CreateValidRequest()
    {
        return new AddCartItemRequestDto
        {
            RoomId = 10,
            CheckIn = DateTime.UtcNow.Date.AddDays(5),
            CheckOut = DateTime.UtcNow.Date.AddDays(7),
            Adults = 2,
            Children = 0
        };
    }
}