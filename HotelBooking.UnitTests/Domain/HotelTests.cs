using HotelBooking.Domain.Entities;

namespace HotelBooking.UnitTests.Domain.Entities;

public class HotelTests
{
    [Fact]
    public void Constructor_WhenDataIsValid_ShouldCreateHotelWithCorrectValues()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var hotel = CreateValidHotel();

        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal("Test Hotel", hotel.Name);
        Assert.Equal("Test Owner", hotel.OwnerName);
        Assert.Equal("Test Address", hotel.Address);
        Assert.Equal(32.2211, hotel.Latitude);
        Assert.Equal(35.2544, hotel.Longitude);
        Assert.Equal((HotelType)1, hotel.HotelType);
        Assert.Equal(10, hotel.CityId);

        Assert.Equal("Test description", hotel.Description);
        Assert.Equal("Test history", hotel.History);

        Assert.True(hotel.IsActive);
        Assert.Null(hotel.UpdatedAt);

        Assert.InRange(
            hotel.CreatedAt,
            beforeCreation,
            afterCreation);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenNameIsInvalid_ShouldThrowArgumentException(
        string name)
    {
        // Act
        var action = () =>
            CreateValidHotel(name: name);

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("name", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenOwnerNameIsInvalid_ShouldThrowArgumentException(
        string ownerName)
    {
        // Act
        var action = () =>
            CreateValidHotel(ownerName: ownerName);

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("ownerName", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenAddressIsInvalid_ShouldThrowArgumentException(
        string address)
    {
        // Act
        var action = () =>
            CreateValidHotel(address: address);

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("address", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenCityIdIsInvalid_ShouldThrowArgumentException(
        int cityId)
    {
        // Act
        var action = () =>
            CreateValidHotel(cityId: cityId);

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("cityId", exception.ParamName);
    }

    [Fact]
    public void Constructor_WhenDescriptionHasWhitespace_ShouldTrimDescription()
    {
        // Act
        var hotel = CreateValidHotel(
            description: "   Test description   ");

        // Assert
        Assert.Equal(
            "Test description",
            hotel.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenDescriptionIsEmptyOrWhitespace_ShouldSetDescriptionToNull(
        string description)
    {
        // Act
        var hotel = CreateValidHotel(
            description: description);

        // Assert
        Assert.Null(hotel.Description);
    }

    [Fact]
    public void Constructor_WhenDescriptionIsNull_ShouldKeepDescriptionNull()
    {
        // Act
        var hotel = CreateValidHotel(
            description: null);

        // Assert
        Assert.Null(hotel.Description);
    }

    [Fact]
    public void Constructor_WhenHistoryHasWhitespace_ShouldTrimHistory()
    {
        // Act
        var hotel = CreateValidHotel(
            history: "   Founded in 1990   ");

        // Assert
        Assert.Equal(
            "Founded in 1990",
            hotel.History);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenHistoryIsEmptyOrWhitespace_ShouldSetHistoryToNull(
        string history)
    {
        // Act
        var hotel = CreateValidHotel(
            history: history);

        // Assert
        Assert.Null(hotel.History);
    }

    [Fact]
    public void Constructor_WhenHistoryIsNull_ShouldKeepHistoryNull()
    {
        // Act
        var hotel = CreateValidHotel(
            history: null);

        // Assert
        Assert.Null(hotel.History);
    }

    [Fact]
    public void Constructor_ShouldInitializeCollectionsAsEmpty()
    {
        // Act
        var hotel = CreateValidHotel();

        // Assert
        Assert.Empty(hotel.Rooms);
        Assert.Empty(hotel.HotelImages);
        Assert.Empty(hotel.Promotions);
        Assert.Empty(hotel.HotelAmenities);
        Assert.Empty(hotel.NearbyAttractions);
        Assert.Empty(hotel.RecentlyVisitedHotels);
        Assert.Empty(hotel.Invoices);
    }

    [Fact]
    public void Update_WhenDataIsValid_ShouldUpdateAllFields()
    {
        // Arrange
        var hotel = CreateValidHotel();

        // Act
        hotel.Update(
            name: "Updated Hotel",
            ownerName: "Updated Owner",
            address: "Updated Address",
            latitude: 31.9,
            longitude: 35.1,
            hotelType: (HotelType)2,
            cityId: 20,
            description: "Updated description",
            history: "Updated history");

        // Assert
        Assert.Equal("Updated Hotel", hotel.Name);
        Assert.Equal("Updated Owner", hotel.OwnerName);
        Assert.Equal("Updated Address", hotel.Address);
        Assert.Equal(31.9, hotel.Latitude);
        Assert.Equal(35.1, hotel.Longitude);
        Assert.Equal((HotelType)2, hotel.HotelType);
        Assert.Equal(20, hotel.CityId);
        Assert.Equal(
            "Updated description",
            hotel.Description);
        Assert.Equal(
            "Updated history",
            hotel.History);
    }

    [Fact]
    public void Update_WhenDataIsValid_ShouldSetUpdatedAt()
    {
        // Arrange
        var hotel = CreateValidHotel();

        var beforeUpdate = DateTime.UtcNow;

        // Act
        UpdateWithValidData(hotel);

        var afterUpdate = DateTime.UtcNow;

        // Assert
        Assert.NotNull(hotel.UpdatedAt);

        Assert.InRange(
            hotel.UpdatedAt!.Value,
            beforeUpdate,
            afterUpdate);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WhenNameIsInvalid_ShouldThrowArgumentException(
        string name)
    {
        // Arrange
        var hotel = CreateValidHotel();

        // Act
        var action = () =>
            UpdateWithValidData(
                hotel,
                name: name);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WhenOwnerNameIsInvalid_ShouldThrowArgumentException(
        string ownerName)
    {
        // Arrange
        var hotel = CreateValidHotel();

        // Act
        var action = () =>
            UpdateWithValidData(
                hotel,
                ownerName: ownerName);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WhenAddressIsInvalid_ShouldThrowArgumentException(
        string address)
    {
        // Arrange
        var hotel = CreateValidHotel();

        // Act
        var action = () =>
            UpdateWithValidData(
                hotel,
                address: address);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Update_WhenCityIdIsInvalid_ShouldThrowArgumentException(
        int cityId)
    {
        // Arrange
        var hotel = CreateValidHotel();

        // Act
        var action = () =>
            UpdateWithValidData(
                hotel,
                cityId: cityId);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Update_ShouldTrimDescriptionAndHistory()
    {
        // Arrange
        var hotel = CreateValidHotel();

        // Act
        UpdateWithValidData(
            hotel,
            description: "   New description   ",
            history: "   New history   ");

        // Assert
        Assert.Equal(
            "New description",
            hotel.Description);

        Assert.Equal(
            "New history",
            hotel.History);
    }

    [Fact]
    public void Update_WhenDescriptionAndHistoryAreWhitespace_ShouldSetThemToNull()
    {
        // Arrange
        var hotel = CreateValidHotel();

        // Act
        UpdateWithValidData(
            hotel,
            description: "   ",
            history: "   ");

        // Assert
        Assert.Null(hotel.Description);
        Assert.Null(hotel.History);
    }

    [Fact]
    public void Update_ShouldNotChangeActiveStatus()
    {
        // Arrange
        var hotel = CreateValidHotel();
        hotel.ChangeStatus(false);

        // Act
        UpdateWithValidData(hotel);

        // Assert
        Assert.False(hotel.IsActive);
    }

    [Fact]
    public void ChangeStatus_WhenFalse_ShouldDeactivateHotel()
    {
        // Arrange
        var hotel = CreateValidHotel();

        // Act
        hotel.ChangeStatus(false);

        // Assert
        Assert.False(hotel.IsActive);
    }

    [Fact]
    public void ChangeStatus_WhenTrue_ShouldActivateHotel()
    {
        // Arrange
        var hotel = CreateValidHotel();

        hotel.ChangeStatus(false);

        // Act
        hotel.ChangeStatus(true);

        // Assert
        Assert.True(hotel.IsActive);
    }

    [Fact]
    public void ChangeStatus_ShouldSetUpdatedAt()
    {
        // Arrange
        var hotel = CreateValidHotel();

        var beforeChange = DateTime.UtcNow;

        // Act
        hotel.ChangeStatus(false);

        var afterChange = DateTime.UtcNow;

        // Assert
        Assert.NotNull(hotel.UpdatedAt);

        Assert.InRange(
            hotel.UpdatedAt!.Value,
            beforeChange,
            afterChange);
    }

    private static Hotel CreateValidHotel(
        string name = "Test Hotel",
        string ownerName = "Test Owner",
        string address = "Test Address",
        double latitude = 32.2211,
        double longitude = 35.2544,
        HotelType hotelType = (HotelType)1,
        int cityId = 10,
        string? description = "Test description",
        string? history = "Test history")
    {
        return new Hotel(
            name,
            ownerName,
            address,
            latitude,
            longitude,
            hotelType,
            cityId,
            description,
            history);
    }

    private static void UpdateWithValidData(
        Hotel hotel,
        string name = "Updated Hotel",
        string ownerName = "Updated Owner",
        string address = "Updated Address",
        double latitude = 31.9,
        double longitude = 35.1,
        HotelType hotelType = (HotelType)2,
        int cityId = 20,
        string? description = "Updated description",
        string? history = "Updated history")
    {
        hotel.Update(
            name,
            ownerName,
            address,
            latitude,
            longitude,
            hotelType,
            cityId,
            description,
            history);
    }
}