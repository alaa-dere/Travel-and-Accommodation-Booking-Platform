using HotelBooking.Application.Cities;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using Moq;

namespace HotelBooking.UnitTests.Cities;

public class UpdateCityTests
{
    private readonly Mock<ICityRepository> _cityRepositoryMock;
    private readonly UpdateCity _service;

    public UpdateCityTests()
    {
        _cityRepositoryMock = new Mock<ICityRepository>();
        _service = new UpdateCity(_cityRepositoryMock.Object);
    }

    [Fact]
    public async Task UpdateCityAsync_WhenCityDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        const int cityId = 10;
        var request = CreateValidRequest();

        _cityRepositoryMock
            .Setup(repository => repository.GetCityByIdAsync(cityId))
            .ReturnsAsync((City?)null);

        // Act
        var action = async () => await _service.UpdateCityAsync(cityId, request);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);

        _cityRepositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateCityAsync_WhenNameIsEmpty_ShouldThrowArgumentException()
    {
        // Arrange
        const int cityId = 10;
        var city = CreateCity(cityId);

        var request = CreateValidRequest();
        request.Name = string.Empty;

        _cityRepositoryMock
            .Setup(repository => repository.GetCityByIdAsync(cityId))
            .ReturnsAsync(city);

        // Act
        var action = async () => await _service.UpdateCityAsync(cityId, request);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);

        _cityRepositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateCityAsync_WhenNameIsWhitespace_ShouldThrowArgumentException()
    {
        // Arrange
        const int cityId = 10;
        var city = CreateCity(cityId);

        var request = CreateValidRequest();
        request.Name = "   ";

        _cityRepositoryMock
            .Setup(repository => repository.GetCityByIdAsync(cityId))
            .ReturnsAsync(city);

        // Act
        var action = async () => await _service.UpdateCityAsync(cityId, request);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);

        _cityRepositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateCityAsync_WhenCountryIsEmpty_ShouldThrowArgumentException()
    {
        // Arrange
        const int cityId = 10;
        var city = CreateCity(cityId);

        var request = CreateValidRequest();
        request.Country = string.Empty;

        _cityRepositoryMock
            .Setup(repository => repository.GetCityByIdAsync(cityId))
            .ReturnsAsync(city);

        // Act
        var action = async () => await _service.UpdateCityAsync(cityId, request);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);

        _cityRepositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateCityAsync_WhenCountryIsWhitespace_ShouldThrowArgumentException()
    {
        // Arrange
        const int cityId = 10;
        var city = CreateCity(cityId);

        var request = CreateValidRequest();
        request.Country = "   ";

        _cityRepositoryMock
            .Setup(repository => repository.GetCityByIdAsync(cityId))
            .ReturnsAsync(city);

        // Act
        var action = async () => await _service.UpdateCityAsync(cityId, request);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(action);

        _cityRepositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateCityAsync_WhenRequestIsValid_ShouldUpdateCity()
    {
        // Arrange
        const int cityId = 10;
        var city = CreateCity(cityId);
        var request = CreateValidRequest();

        _cityRepositoryMock
            .Setup(repository => repository.GetCityByIdAsync(cityId))
            .ReturnsAsync(city);

        // Act
        await _service.UpdateCityAsync(cityId, request);

        // Assert
        Assert.Equal(request.Name, city.Name);
        Assert.Equal(request.Country, city.Country);
        Assert.Equal(request.PostOffice, city.PostOffice);
        Assert.NotNull(city.UpdatedAt);
    }

    [Fact]
    public async Task UpdateCityAsync_WhenRequestIsValid_ShouldSaveChanges()
    {
        // Arrange
        const int cityId = 10;
        var city = CreateCity(cityId);
        var request = CreateValidRequest();

        _cityRepositoryMock
            .Setup(repository => repository.GetCityByIdAsync(cityId))
            .ReturnsAsync(city);

        // Act
        await _service.UpdateCityAsync(cityId, request);

        // Assert
        _cityRepositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateCityAsync_WhenRequestIsValid_ShouldReturnMappedResponse()
    {
        // Arrange
        const int cityId = 10;
        var city = CreateCity(cityId);
        var request = CreateValidRequest();

        _cityRepositoryMock
            .Setup(repository => repository.GetCityByIdAsync(cityId))
            .ReturnsAsync(city);

        // Act
        var result = await _service.UpdateCityAsync(cityId, request);

        // Assert
        Assert.Equal(cityId, result.CityId);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.Country, result.Country);
        Assert.Equal(request.PostOffice, result.PostOffice);
    }

    [Fact]
    public async Task UpdateCityAsync_ShouldRequestCorrectCityFromRepository()
    {
        // Arrange
        const int cityId = 10;
        var city = CreateCity(cityId);
        var request = CreateValidRequest();

        _cityRepositoryMock
            .Setup(repository => repository.GetCityByIdAsync(cityId))
            .ReturnsAsync(city);

        // Act
        await _service.UpdateCityAsync(cityId, request);

        // Assert
        _cityRepositoryMock.Verify(repository => repository.GetCityByIdAsync(cityId), Times.Once);
    }

    private static City CreateCity(int cityId)
    {
        return new City(
            name: "Old City Name",
            country: "Old Country",
            postOffice: "OLD-100")
        {
            CityId = cityId
        };
    }

    private static CityRequestDto CreateValidRequest()
    {
        return new CityRequestDto
        {
            Name = "Updated City",
            Country = "Updated Country",
            PostOffice = "NEW-200"
        };
    }
}