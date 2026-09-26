using HotelBooking.Domain.Entities;

namespace HotelBooking.UnitTests.Domain.Entities;

public class CityTests
{
    [Fact]
    public void Constructor_WhenDataIsValid_ShouldCreateCityWithCorrectValues()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var city = new City(
            "Nablus",
            "Palestine",
            "P400");

        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal("Nablus", city.Name);
        Assert.Equal("Palestine", city.Country);
        Assert.Equal("P400", city.PostOffice);

        Assert.InRange(
            city.CreatedAt,
            beforeCreation,
            afterCreation);

        Assert.Null(city.UpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenNameIsInvalid_ShouldThrowArgumentException(
        string name)
    {
        // Act
        var action = () =>
            new City(
                name,
                "Palestine",
                "P400");

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("name", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenCountryIsInvalid_ShouldThrowArgumentException(
        string country)
    {
        // Act
        var action = () =>
            new City(
                "Nablus",
                country,
                "P400");

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("country", exception.ParamName);
    }

    [Fact]
    public void Constructor_ShouldInitializeHotelsAsEmptyCollection()
    {
        // Act
        var city = CreateValidCity();

        // Assert
        Assert.NotNull(city.Hotels);
        Assert.Empty(city.Hotels);
    }

    [Fact]
    public void Update_WhenDataIsValid_ShouldUpdateAllFields()
    {
        // Arrange
        var city = CreateValidCity();

        // Act
        city.Update(
            "Ramallah",
            "Palestine",
            "P600");

        // Assert
        Assert.Equal("Ramallah", city.Name);
        Assert.Equal("Palestine", city.Country);
        Assert.Equal("P600", city.PostOffice);
    }

    [Fact]
    public void Update_WhenDataIsValid_ShouldSetUpdatedAt()
    {
        // Arrange
        var city = CreateValidCity();

        var beforeUpdate = DateTime.UtcNow;

        // Act
        city.Update(
            "Ramallah",
            "Palestine",
            "P600");

        var afterUpdate = DateTime.UtcNow;

        // Assert
        Assert.NotNull(city.UpdatedAt);

        Assert.InRange(
            city.UpdatedAt!.Value,
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
        var city = CreateValidCity();

        // Act
        var action = () =>
            city.Update(
                name,
                "Palestine",
                "P600");

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("name", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WhenCountryIsInvalid_ShouldThrowArgumentException(
        string country)
    {
        // Arrange
        var city = CreateValidCity();

        // Act
        var action = () =>
            city.Update(
                "Ramallah",
                country,
                "P600");

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("country", exception.ParamName);
    }

    [Fact]
    public void Update_WhenValidationFails_ShouldNotSetUpdatedAt()
    {
        // Arrange
        var city = CreateValidCity();

        // Act
        try
        {
            city.Update(
                "",
                "Palestine",
                "P600");
        }
        catch (ArgumentException)
        {
        }

        // Assert
        Assert.Null(city.UpdatedAt);
    }

    [Fact]
    public void Update_WhenValidationFails_ShouldNotChangeExistingValues()
    {
        // Arrange
        var city = CreateValidCity();

        // Act
        try
        {
            city.Update(
                "",
                "New Country",
                "NEW");
        }
        catch (ArgumentException)
        {
        }

        // Assert
        Assert.Equal("Nablus", city.Name);
        Assert.Equal("Palestine", city.Country);
        Assert.Equal("P400", city.PostOffice);
    }

    private static City CreateValidCity()
    {
        return new City(
            "Nablus",
            "Palestine",
            "P400");
    }
}