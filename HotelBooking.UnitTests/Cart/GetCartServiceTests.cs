using HotelBooking.Application.Cart;
using HotelBooking.Application.Cart.Retrive;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using Moq;

namespace HotelBooking.UnitTests.Cart;

public class GetCartServiceTests
{
    private readonly Mock<ICartRepository> _cartRepositoryMock;
    private readonly GetCartService _service;

    public GetCartServiceTests()
    {
        _cartRepositoryMock = new Mock<ICartRepository>();

        _service = new GetCartService(_cartRepositoryMock.Object);
    }

    [Fact]
    public async Task GetCartAsync_WhenCartIsEmpty_ShouldReturnEmptyCollection()
    {
        // Arrange
        const int userId = 1;

        _cartRepositoryMock
            .Setup(repository => repository.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<CartItem>());

        // Act
        var result = await _service.GetCartAsync(userId);

        // Assert
        Assert.Empty(result);

        _cartRepositoryMock.Verify(repository => repository.GetByUserIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetCartAsync_WhenCartContainsItem_ShouldMapItemCorrectly()
    {
        // Arrange
        const int userId = 1;

        var cartItem = CreateCartItem(
            cartItemId: 10,
            userId: userId,
            roomId: 20,
            hotelId: 30,
            roomNumber: "101",
            hotelName: "Test Hotel");

        _cartRepositoryMock
            .Setup(repository => repository.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<CartItem> { cartItem });

        // Act
        var result = (await _service.GetCartAsync(userId)).ToList();

        // Assert
        Assert.Single(result);

        var item = result[0];

        Assert.Equal(cartItem.CartItemId, item.CartItemId);
        Assert.Equal(cartItem.RoomId, item.RoomId);
        Assert.Equal(cartItem.Room!.RoomNumber, item.RoomNumber);
        Assert.Equal(cartItem.Room.HotelId, item.HotelId);
        Assert.Equal(cartItem.Room.Hotel!.Name, item.HotelName);
        Assert.Equal(cartItem.CheckIn, item.CheckIn);
        Assert.Equal(cartItem.CheckOut, item.CheckOut);
        Assert.Equal(cartItem.Adults, item.Adults);
        Assert.Equal(cartItem.Children, item.Children);
    }

    [Fact]
    public async Task GetCartAsync_WhenCartContainsMultipleItems_ShouldReturnAllItems()
    {
        // Arrange
        const int userId = 1;

        var firstItem = CreateCartItem(
            cartItemId: 10,
            userId: userId,
            roomId: 20,
            hotelId: 30,
            roomNumber: "101",
            hotelName: "First Hotel");

        var secondItem = CreateCartItem(
            cartItemId: 11,
            userId: userId,
            roomId: 21,
            hotelId: 40,
            roomNumber: "202",
            hotelName: "Second Hotel");

        _cartRepositoryMock
            .Setup(repository => repository.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<CartItem> { firstItem, secondItem });

        // Act
        var result = (await _service.GetCartAsync(userId)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.CartItemId == firstItem.CartItemId &&
                                        item.RoomId == firstItem.RoomId &&
                                        item.HotelName == "First Hotel");

        Assert.Contains(result, item => item.CartItemId == secondItem.CartItemId &&
                                        item.RoomId == secondItem.RoomId &&
                                        item.HotelName == "Second Hotel");
    }

    [Fact]
    public async Task GetCartAsync_ShouldRequestCartForCorrectUser()
    {
        // Arrange
        const int userId = 15;
        _cartRepositoryMock.Setup(repository => repository.GetByUserIdAsync(userId)).ReturnsAsync(new List<CartItem>());

        // Act
        await _service.GetCartAsync(userId);

        // Assert
        _cartRepositoryMock.Verify(repository => repository.GetByUserIdAsync(userId), Times.Once);
        _cartRepositoryMock.Verify(repository => repository.GetByUserIdAsync(It.Is<int>(id => id != userId)), Times.Never);
    }

    private static CartItem CreateCartItem(int cartItemId, int userId, int roomId, int hotelId, string roomNumber, string hotelName)
    {
        var hotel = new Hotel(
            name: hotelName,
            ownerName: "Test Owner",
            address: "Test Address",
            latitude: 0,
            longitude: 0,
            hotelType: (HotelType)1,
            cityId: 1,
            description: null,
            history: null)
        {
            HotelId = hotelId
        };

        var room = new Room(
            roomNumber: roomNumber,
            roomType: (RoomType)1,
            pricePerNight: 100m,
            adultsCapacity: 2,
            childCapacity: 2,
            hotelId: hotelId,
            description: null)
        {
            RoomId = roomId,
            Hotel = hotel
        };

        var checkIn = DateTime.UtcNow.Date.AddDays(5);
        var checkOut = checkIn.AddDays(2);

        return new CartItem(
            userId: userId,
            roomId: roomId,
            checkIn: checkIn,
            checkOut: checkOut,
            adults: 2,
            children: 1)
        {
            CartItemId = cartItemId,
            Room = room
        };
    }
}