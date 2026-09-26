using HotelBooking.Application.Cities;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using Moq;

namespace HotelBooking.UnitTests.Cities;

public class CreateCityTests
{
    private readonly Mock<ICityRepository> _cityRepositoryMock;
    private readonly CreateCity _service;

    public CreateCityTests()
    {
        _cityRepositoryMock = new Mock<ICityRepository>();
        _service = new CreateCity(_cityRepositoryMock.Object);
    }

    [Fact]
    public async Task CreateCityAsync_WhenRequestIsValid_ShouldAddCity()
    {
        // Arrange
        var request = CreateValidRequest();

        City? addedCity = null;

        _cityRepositoryMock.Setup(repository => repository.Add(It.IsAny<City>())).Callback<City>(city => addedCity = city);

        // Act
        await _service.CreateCityAsync(request);

        // Assert
        Assert.NotNull(addedCity);

        Assert.Equal(request.Name, addedCity!.Name);
        Assert.Equal(request.Country, addedCity.Country);
        Assert.Equal(request.PostOffice, addedCity.PostOffice);

        _cityRepositoryMock.Verify(repository => repository.Add( It.IsAny<City>()), Times.Once);
    }

    [Fact]
    public async Task CreateCityAsync_WhenRequestIsValid_ShouldSaveChanges()
    {
        // Arrange
        var request = CreateValidRequest();

        // Act
        await _service.CreateCityAsync(request);

        // Assert
        _cityRepositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateCityAsync_WhenRequestIsValid_ShouldReturnMappedResponse()
    {
        // Arrange
        var request = CreateValidRequest();

        _cityRepositoryMock.Setup(repository => repository.Add(It.IsAny<City>()))
            .Callback<City>(city => { city.CityId = 10; });

        // Act
        var result = await _service.CreateCityAsync(request);

        // Assert
        Assert.Equal(10, result.CityId);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.Country, result.Country);
        Assert.Equal(request.PostOffice, result.PostOffice);
    }

    [Fact]
    public async Task CreateCityAsync_WhenNameIsEmpty_ShouldThrowArgumentException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Name = string.Empty;

        // Act
        var action = async () => await _service.CreateCityAsync(request);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);

        _cityRepositoryMock.Verify(repository => repository.Add(It.IsAny<City>()), Times.Never);
        _cityRepositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateCityAsync_WhenNameIsWhitespace_ShouldThrowArgumentException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Name = "   ";

        // Act
        var action = async () => await _service.CreateCityAsync(request);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);

        _cityRepositoryMock.Verify(repository => repository.Add(It.IsAny<City>()), Times.Never);
        _cityRepositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateCityAsync_WhenCountryIsEmpty_ShouldThrowArgumentException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Country = string.Empty;

        // Act
        var action = async () => await _service.CreateCityAsync(request);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);

        _cityRepositoryMock.Verify(repository => repository.Add(It.IsAny<City>()), Times.Never);
        _cityRepositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateCityAsync_WhenCountryIsWhitespace_ShouldThrowArgumentException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Country = "   ";

        // Act
        var action = async () => await _service.CreateCityAsync(request);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);

        _cityRepositoryMock.Verify(repository => repository.Add(It.IsAny<City>()), Times.Never);
        _cityRepositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Never);
    }

    private static CityRequestDto CreateValidRequest()
    {
        return new CityRequestDto
        {
            Name = "Nablus",
            Country = "Palestine",
            PostOffice = "P400"
        };
    }
}