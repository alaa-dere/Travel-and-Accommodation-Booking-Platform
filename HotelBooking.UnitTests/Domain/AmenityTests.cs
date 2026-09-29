using HotelBooking.Domain.Entities;

namespace HotelBooking.UnitTests.Domain.Entities;

public class AmenityTests
{
    [Fact]
    public void Constructor_WhenDataIsValid_ShouldCreateAmenity()
    {
        // Act
        var amenity = new Amenity(
            "WiFi",
            "Free high-speed internet");

        // Assert
        Assert.Equal("WiFi", amenity.Name);
        Assert.Equal(
            "Free high-speed internet",
            amenity.Description);

        Assert.Equal(0, amenity.AmenityId);
        Assert.Empty(amenity.HotelAmenities);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenNameIsInvalid_ShouldThrowArgumentException(
        string name)
    {
        // Act
        var action = () =>
            new Amenity(name, "Description");

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal(
            "Amenity name is required.",
            exception.Message);
    }

    [Fact]
    public void Constructor_ShouldTrimName()
    {
        // Act
        var amenity = new Amenity(
            "   Swimming Pool   ",
            "Description");

        // Assert
        Assert.Equal(
            "Swimming Pool",
            amenity.Name);
    }

    [Fact]
    public void Constructor_ShouldTrimDescription()
    {
        // Act
        var amenity = new Amenity(
            "WiFi",
            "   Free internet   ");

        // Assert
        Assert.Equal(
            "Free internet",
            amenity.Description);
    }

    [Fact]
    public void Constructor_WhenDescriptionIsNull_ShouldSetDescriptionToNull()
    {
        // Act
        var amenity = new Amenity(
            "WiFi",
            null);

        // Assert
        Assert.Null(amenity.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenDescriptionIsEmptyOrWhitespace_ShouldSetDescriptionToNull(
        string description)
    {
        // Act
        var amenity = new Amenity(
            "WiFi",
            description);

        // Assert
        Assert.Null(amenity.Description);
    }

    [Fact]
    public void Constructor_ShouldInitializeHotelAmenitiesAsEmptyCollection()
    {
        // Act
        var amenity = new Amenity(
            "WiFi",
            null);

        // Assert
        Assert.NotNull(amenity.HotelAmenities);
        Assert.Empty(amenity.HotelAmenities);
    }
}