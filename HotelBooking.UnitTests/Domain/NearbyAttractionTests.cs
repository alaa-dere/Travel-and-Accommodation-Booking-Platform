using HotelBooking.Domain.Entities;

namespace HotelBooking.UnitTests.Domain.Entities;

public class NearbyAttractionTests
{
    [Fact]
    public void Constructor_WhenDataIsValid_ShouldCreateNearbyAttractionWithCorrectValues()
    {
        // Act
        var attraction = new NearbyAttraction(
            hotelId: 1,
            name: "Old City",
            description: "Historic area",
            latitude: 32.2211,
            longitude: 35.2544);

        // Assert
        Assert.Equal(1, attraction.HotelId);
        Assert.Equal("Old City", attraction.Name);
        Assert.Equal("Historic area", attraction.Description);
        Assert.Equal(32.2211, attraction.Latitude);
        Assert.Equal(35.2544, attraction.Longitude);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenHotelIdIsInvalid_ShouldThrowArgumentException(
        int hotelId)
    {
        // Act
        var action = () =>
            CreateValidAttraction(hotelId: hotelId);

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("hotelId", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenNameIsInvalid_ShouldThrowArgumentException(
        string name)
    {
        // Act
        var action = () =>
            CreateValidAttraction(name: name);

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Constructor_WhenNameExceeds100Characters_ShouldThrowArgumentException()
    {
        // Arrange
        var name = new string('A', 101);

        // Act
        var action = () =>
            CreateValidAttraction(name: name);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Constructor_WhenNameIsExactly100Characters_ShouldSucceed()
    {
        // Arrange
        var name = new string('A', 100);

        // Act
        var attraction =
            CreateValidAttraction(name: name);

        // Assert
        Assert.Equal(name, attraction.Name);
    }

    [Fact]
    public void Constructor_WhenDescriptionExceeds500Characters_ShouldThrowArgumentException()
    {
        // Arrange
        var description = new string('A', 501);

        // Act
        var action = () =>
            CreateValidAttraction(
                description: description);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Constructor_WhenDescriptionIsExactly500Characters_ShouldSucceed()
    {
        // Arrange
        var description = new string('A', 500);

        // Act
        var attraction =
            CreateValidAttraction(
                description: description);

        // Assert
        Assert.Equal(
            description,
            attraction.Description);
    }

    [Theory]
    [InlineData(-90.1)]
    [InlineData(90.1)]
    public void Constructor_WhenLatitudeIsOutsideRange_ShouldThrowArgumentOutOfRangeException(
        double latitude)
    {
        // Act
        var action = () =>
            CreateValidAttraction(
                latitude: latitude);

        // Assert
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(action);

        Assert.Equal("latitude", exception.ParamName);
    }

    [Theory]
    [InlineData(-90)]
    [InlineData(90)]
    public void Constructor_WhenLatitudeIsAtBoundary_ShouldSucceed(
        double latitude)
    {
        // Act
        var attraction =
            CreateValidAttraction(
                latitude: latitude);

        // Assert
        Assert.Equal(latitude, attraction.Latitude);
    }

    [Theory]
    [InlineData(-180.1)]
    [InlineData(180.1)]
    public void Constructor_WhenLongitudeIsOutsideRange_ShouldThrowArgumentOutOfRangeException(
        double longitude)
    {
        // Act
        var action = () =>
            CreateValidAttraction(
                longitude: longitude);

        // Assert
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(action);

        Assert.Equal("longitude", exception.ParamName);
    }

    [Theory]
    [InlineData(-180)]
    [InlineData(180)]
    public void Constructor_WhenLongitudeIsAtBoundary_ShouldSucceed(
        double longitude)
    {
        // Act
        var attraction =
            CreateValidAttraction(
                longitude: longitude);

        // Assert
        Assert.Equal(
            longitude,
            attraction.Longitude);
    }

    [Fact]
    public void Constructor_ShouldTrimNameAndDescription()
    {
        // Act
        var attraction = new NearbyAttraction(
            hotelId: 1,
            name: "   Old City   ",
            description: "   Historic area   ",
            latitude: 32.2211,
            longitude: 35.2544);

        // Assert
        Assert.Equal("Old City", attraction.Name);
        Assert.Equal(
            "Historic area",
            attraction.Description);
    }

    [Fact]
    public void Constructor_WhenDescriptionIsNull_ShouldKeepDescriptionNull()
    {
        // Act
        var attraction =
            CreateValidAttraction(
                description: null);

        // Assert
        Assert.Null(attraction.Description);
    }

    [Fact]
    public void Update_WhenDataIsValid_ShouldUpdateAllFields()
    {
        // Arrange
        var attraction = CreateValidAttraction();

        // Act
        attraction.Update(
            name: "New Attraction",
            description: "New description",
            latitude: 31.9,
            longitude: 35.1);

        // Assert
        Assert.Equal(
            "New Attraction",
            attraction.Name);

        Assert.Equal(
            "New description",
            attraction.Description);

        Assert.Equal(31.9, attraction.Latitude);
        Assert.Equal(35.1, attraction.Longitude);
    }

    [Fact]
    public void Update_ShouldTrimNameAndDescription()
    {
        // Arrange
        var attraction = CreateValidAttraction();

        // Act
        attraction.Update(
            name: "   New Attraction   ",
            description: "   New description   ",
            latitude: 31.9,
            longitude: 35.1);

        // Assert
        Assert.Equal(
            "New Attraction",
            attraction.Name);

        Assert.Equal(
            "New description",
            attraction.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WhenNameIsInvalid_ShouldThrowArgumentException(
        string name)
    {
        // Arrange
        var attraction = CreateValidAttraction();

        // Act
        var action = () =>
            attraction.Update(
                name,
                "Description",
                32.0,
                35.0);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Update_WhenNameExceeds100Characters_ShouldThrowArgumentException()
    {
        // Arrange
        var attraction = CreateValidAttraction();

        // Act
        var action = () =>
            attraction.Update(
                new string('A', 101),
                "Description",
                32.0,
                35.0);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Update_WhenDescriptionExceeds500Characters_ShouldThrowArgumentException()
    {
        // Arrange
        var attraction = CreateValidAttraction();

        // Act
        var action = () =>
            attraction.Update(
                "Attraction",
                new string('A', 501),
                32.0,
                35.0);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Theory]
    [InlineData(-90.1)]
    [InlineData(90.1)]
    public void Update_WhenLatitudeIsOutsideRange_ShouldThrowArgumentOutOfRangeException(
        double latitude)
    {
        // Arrange
        var attraction = CreateValidAttraction();

        // Act
        var action = () =>
            attraction.Update(
                "Attraction",
                "Description",
                latitude,
                35.0);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(
            action);
    }

    [Theory]
    [InlineData(-180.1)]
    [InlineData(180.1)]
    public void Update_WhenLongitudeIsOutsideRange_ShouldThrowArgumentOutOfRangeException(
        double longitude)
    {
        // Arrange
        var attraction = CreateValidAttraction();

        // Act
        var action = () =>
            attraction.Update(
                "Attraction",
                "Description",
                32.0,
                longitude);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(
            action);
    }

    [Theory]
    [InlineData(-90, -180)]
    [InlineData(90, 180)]
    public void Update_WhenCoordinatesAreAtBoundaries_ShouldSucceed(
        double latitude,
        double longitude)
    {
        // Arrange
        var attraction = CreateValidAttraction();

        // Act
        attraction.Update(
            "Attraction",
            "Description",
            latitude,
            longitude);

        // Assert
        Assert.Equal(latitude, attraction.Latitude);
        Assert.Equal(longitude, attraction.Longitude);
    }

    [Fact]
    public void Update_WhenDescriptionIsNull_ShouldSetDescriptionToNull()
    {
        // Arrange
        var attraction = CreateValidAttraction();

        // Act
        attraction.Update(
            "Attraction",
            null,
            32.0,
            35.0);

        // Assert
        Assert.Null(attraction.Description);
    }

    [Fact]
    public void Update_WhenValidationFails_ShouldNotChangeExistingValues()
    {
        // Arrange
        var attraction = CreateValidAttraction();

        var originalName = attraction.Name;
        var originalDescription = attraction.Description;
        var originalLatitude = attraction.Latitude;
        var originalLongitude = attraction.Longitude;

        // Act
        try
        {
            attraction.Update(
                "",
                "New description",
                40,
                50);
        }
        catch (ArgumentException)
        {
        }

        // Assert
        Assert.Equal(originalName, attraction.Name);
        Assert.Equal(
            originalDescription,
            attraction.Description);
        Assert.Equal(
            originalLatitude,
            attraction.Latitude);
        Assert.Equal(
            originalLongitude,
            attraction.Longitude);
    }

    private static NearbyAttraction CreateValidAttraction(
        int hotelId = 1,
        string name = "Old City",
        string? description = "Historic area",
        double latitude = 32.2211,
        double longitude = 35.2544)
    {
        return new NearbyAttraction(
            hotelId,
            name,
            description,
            latitude,
            longitude);
    }
}