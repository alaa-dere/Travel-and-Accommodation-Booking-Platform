using HotelBooking.Domain.Entities;

namespace HotelBooking.UnitTests.Domain.Entities;

public class CartItemTests
{
    [Fact]
    public void Constructor_WhenDataIsValid_ShouldCreateCartItemWithCorrectValues()
    {
        // Arrange
        var checkIn = DateTime.UtcNow.Date.AddDays(2);
        var checkOut = DateTime.UtcNow.Date.AddDays(5);

        var beforeCreation = DateTime.UtcNow;

        // Act
        var cartItem = new CartItem(
            userId: 1,
            roomId: 10,
            checkIn: checkIn,
            checkOut: checkOut,
            adults: 2,
            children: 1);

        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(1, cartItem.UserId);
        Assert.Equal(10, cartItem.RoomId);
        Assert.Equal(checkIn, cartItem.CheckIn);
        Assert.Equal(checkOut, cartItem.CheckOut);
        Assert.Equal(2, cartItem.Adults);
        Assert.Equal(1, cartItem.Children);

        Assert.InRange(
            cartItem.CreatedAt,
            beforeCreation,
            afterCreation);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenUserIdIsInvalid_ShouldThrowArgumentException(
        int userId)
    {
        // Act
        var action = () =>
            CreateValidCartItem(userId: userId);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenRoomIdIsInvalid_ShouldThrowArgumentException(
        int roomId)
    {
        // Act
        var action = () =>
            CreateValidCartItem(roomId: roomId);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Constructor_WhenCheckInIsBeforeToday_ShouldThrowArgumentException()
    {
        // Arrange
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);

        // Act
        var action = () =>
            CreateValidCartItem(
                checkIn: yesterday,
                checkOut: DateTime.UtcNow.Date.AddDays(2));

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Constructor_WhenCheckInIsToday_ShouldSucceed()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        // Act
        var cartItem = CreateValidCartItem(
            checkIn: today,
            checkOut: tomorrow);

        // Assert
        Assert.Equal(today, cartItem.CheckIn);
        Assert.Equal(tomorrow, cartItem.CheckOut);
    }

    [Fact]
    public void Constructor_WhenCheckOutIsBeforeCheckIn_ShouldThrowArgumentException()
    {
        // Arrange
        var checkIn = DateTime.UtcNow.Date.AddDays(3);
        var checkOut = DateTime.UtcNow.Date.AddDays(2);

        // Act
        var action = () =>
            CreateValidCartItem(
                checkIn: checkIn,
                checkOut: checkOut);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Constructor_WhenCheckOutIsSameDateAsCheckIn_ShouldThrowArgumentException()
    {
        // Arrange
        var checkIn =
            DateTime.UtcNow.Date.AddDays(2).AddHours(10);

        var checkOut =
            DateTime.UtcNow.Date.AddDays(2).AddHours(20);

        // Act
        var action = () =>
            CreateValidCartItem(
                checkIn: checkIn,
                checkOut: checkOut);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Constructor_WhenCheckOutIsNextDay_ShouldSucceed()
    {
        // Arrange
        var checkIn = DateTime.UtcNow.Date.AddDays(2);
        var checkOut = checkIn.AddDays(1);

        // Act
        var cartItem = CreateValidCartItem(
            checkIn: checkIn,
            checkOut: checkOut);

        // Assert
        Assert.Equal(checkIn, cartItem.CheckIn);
        Assert.Equal(checkOut, cartItem.CheckOut);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenAdultsIsInvalid_ShouldThrowArgumentException(
        int adults)
    {
        // Act
        var action = () =>
            CreateValidCartItem(adults: adults);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Constructor_WhenAdultsIsOne_ShouldSucceed()
    {
        // Act
        var cartItem =
            CreateValidCartItem(adults: 1);

        // Assert
        Assert.Equal(1, cartItem.Adults);
    }

    [Fact]
    public void Constructor_WhenChildrenIsNegative_ShouldThrowArgumentException()
    {
        // Act
        var action = () =>
            CreateValidCartItem(children: -1);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Constructor_WhenChildrenIsZero_ShouldSucceed()
    {
        // Act
        var cartItem =
            CreateValidCartItem(children: 0);

        // Assert
        Assert.Equal(0, cartItem.Children);
    }

    private static CartItem CreateValidCartItem(
        int userId = 1,
        int roomId = 10,
        DateTime? checkIn = null,
        DateTime? checkOut = null,
        int adults = 2,
        int children = 1)
    {
        var validCheckIn =
            checkIn ?? DateTime.UtcNow.Date.AddDays(2);

        var validCheckOut =
            checkOut ?? validCheckIn.AddDays(3);

        return new CartItem(
            userId,
            roomId,
            validCheckIn,
            validCheckOut,
            adults,
            children);
    }
}