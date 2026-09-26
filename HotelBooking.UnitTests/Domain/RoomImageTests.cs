using HotelBooking.Domain.Entities;

namespace HotelBooking.UnitTests.Domain.Entities;

public class RoomImageTests
{
    [Fact]
    public void Constructor_WhenDataIsValid_ShouldCreateRoomImageWithCorrectValues()
    {
        // Act
        var roomImage = new RoomImage(
            imageUrl: "https://example.com/room.jpg",
            displayOrder: 1,
            roomId: 10);

        // Assert
        Assert.Equal(
            "https://example.com/room.jpg",
            roomImage.ImageUrl);

        Assert.Equal(1, roomImage.DisplayOrder);
        Assert.Equal(10, roomImage.RoomId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenImageUrlIsInvalid_ShouldThrowArgumentException(
        string imageUrl)
    {
        // Act
        var action = () =>
            new RoomImage(
                imageUrl,
                displayOrder: 1,
                roomId: 10);

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("imageUrl", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenDisplayOrderIsInvalid_ShouldThrowArgumentException(
        int displayOrder)
    {
        // Act
        var action = () =>
            new RoomImage(
                "https://example.com/room.jpg",
                displayOrder,
                roomId: 10);

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("displayOrder", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenRoomIdIsInvalid_ShouldThrowArgumentException(
        int roomId)
    {
        // Act
        var action = () =>
            new RoomImage(
                "https://example.com/room.jpg",
                displayOrder: 1,
                roomId);

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("roomId", exception.ParamName);
    }

    [Fact]
    public void Constructor_WhenDisplayOrderIsOne_ShouldSucceed()
    {
        // Act
        var roomImage = new RoomImage(
            "https://example.com/room.jpg",
            displayOrder: 1,
            roomId: 10);

        // Assert
        Assert.Equal(1, roomImage.DisplayOrder);
    }

    [Fact]
    public void Constructor_WhenRoomIdIsOne_ShouldSucceed()
    {
        // Act
        var roomImage = new RoomImage(
            "https://example.com/room.jpg",
            displayOrder: 1,
            roomId: 1);

        // Assert
        Assert.Equal(1, roomImage.RoomId);
    }

    [Fact]
    public void Constructor_ShouldHaveDefaultRoomImageId()
    {
        // Act
        var roomImage = CreateValidRoomImage();

        // Assert
        Assert.Equal(0, roomImage.RoomImageId);
    }

    [Fact]
    public void Constructor_ShouldHaveNullRoomByDefault()
    {
        // Act
        var roomImage = CreateValidRoomImage();

        // Assert
        Assert.Null(roomImage.Room);
    }

    private static RoomImage CreateValidRoomImage()
    {
        return new RoomImage(
            imageUrl: "https://example.com/room.jpg",
            displayOrder: 1,
            roomId: 10);
    }
}