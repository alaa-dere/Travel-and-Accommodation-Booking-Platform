using HotelBooking.Domain.Entities;

namespace HotelBooking.UnitTests.Domain.Entities;

public class HotelImageTests
{
    [Fact]
    public void Constructor_WhenDataIsValid_ShouldCreateHotelImageWithCorrectValues()
    {
        // Act
        var image = new HotelImage(
            imageUrl: "https://example.com/hotel.jpg",
            displayOrder: 1,
            hotelId: 10);

        // Assert
        Assert.Equal(
            "https://example.com/hotel.jpg",
            image.ImageUrl);

        Assert.Equal(1, image.DisplayOrder);
        Assert.Equal(10, image.HotelId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenImageUrlIsInvalid_ShouldThrowArgumentException(
        string imageUrl)
    {
        // Act
        var action = () =>
            new HotelImage(
                imageUrl,
                displayOrder: 1,
                hotelId: 10);

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
            new HotelImage(
                "https://example.com/hotel.jpg",
                displayOrder,
                hotelId: 10);

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("displayOrder", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenHotelIdIsInvalid_ShouldThrowArgumentException(
        int hotelId)
    {
        // Act
        var action = () =>
            new HotelImage(
                "https://example.com/hotel.jpg",
                displayOrder: 1,
                hotelId);

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("hotelId", exception.ParamName);
    }

    [Fact]
    public void Constructor_WhenDisplayOrderIsOne_ShouldSucceed()
    {
        // Act
        var image = new HotelImage(
            "https://example.com/hotel.jpg",
            displayOrder: 1,
            hotelId: 10);

        // Assert
        Assert.Equal(1, image.DisplayOrder);
    }

    [Fact]
    public void Constructor_WhenHotelIdIsOne_ShouldSucceed()
    {
        // Act
        var image = new HotelImage(
            "https://example.com/hotel.jpg",
            displayOrder: 1,
            hotelId: 1);

        // Assert
        Assert.Equal(1, image.HotelId);
    }
}