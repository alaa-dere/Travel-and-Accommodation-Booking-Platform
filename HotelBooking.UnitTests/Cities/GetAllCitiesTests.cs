using HotelBooking.Application.Cities;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using Moq;

namespace HotelBooking.UnitTests.Cities;

public class GetAllCitiesTests
{
    private readonly Mock<ICityRepository> _cityRepositoryMock;
    private readonly GetAllCities _service;

    public GetAllCitiesTests()
    {
        _cityRepositoryMock = new Mock<ICityRepository>();
        _service = new GetAllCities(_cityRepositoryMock.Object);
    }

    [Fact]
    public async Task GetAllCitiesAsync_WhenNoCitiesExist_ShouldReturnEmptyCollection()
    {
        // Arrange
        const string? search = null;

        _cityRepositoryMock
            .Setup(repository => repository.GetCitiesAsync(search))
            .ReturnsAsync(new List<City>());

        // Act
        var result = await _service.GetAllCitiesAsync(search);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllCitiesAsync_WhenCityExists_ShouldMapCityCorrectly()
    {
        // Arrange
        var city = CreateCity(
            cityId: 1,
            name: "Nablus",
            country: "Palestine",
            postOffice: "P400");

        _cityRepositoryMock
            .Setup(repository => repository.GetCitiesAsync(null))
            .ReturnsAsync(new List<City> { city });

        // Act
        var result = (await _service.GetAllCitiesAsync(null)).ToList();

        // Assert
        Assert.Single(result);

        var response = result[0];

        Assert.Equal(city.CityId, response.CityId);
        Assert.Equal(city.Name, response.Name);
        Assert.Equal(city.Country, response.Country);
        Assert.Equal(city.PostOffice, response.PostOffice);
    }

    [Fact]
    public async Task GetAllCitiesAsync_WhenMultipleCitiesExist_ShouldReturnAllCities()
    {
        // Arrange
        var cities = new List<City>
        {
            CreateCity(
                cityId: 1,
                name: "Nablus",
                country: "Palestine",
                postOffice: "P400"),

            CreateCity(
                cityId: 2,
                name: "Ramallah",
                country: "Palestine",
                postOffice: "P600")
        };

        _cityRepositoryMock
            .Setup(repository => repository.GetCitiesAsync(null))
            .ReturnsAsync(cities);

        // Act
        var result = (await _service.GetAllCitiesAsync(null)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].CityId);
        Assert.Equal("Nablus", result[0].Name);
        Assert.Equal(2, result[1].CityId);
        Assert.Equal("Ramallah", result[1].Name);
    }

    [Fact]
    public async Task GetAllCitiesAsync_WhenSearchIsProvided_ShouldPassSearchToRepository()
    {
        // Arrange
        const string search = "Nablus";

        _cityRepositoryMock
            .Setup(repository => repository.GetCitiesAsync(search))
            .ReturnsAsync(new List<City>());

        // Act
        await _service.GetAllCitiesAsync(search);

        // Assert
        _cityRepositoryMock.Verify(repository => repository.GetCitiesAsync(search), Times.Once);
    }

    [Fact]
    public async Task GetAllCitiesAsync_WhenSearchIsNull_ShouldPassNullToRepository()
    {
        // Arrange
        _cityRepositoryMock
            .Setup(repository => repository.GetCitiesAsync(null))
            .ReturnsAsync(new List<City>());

        // Act
        await _service.GetAllCitiesAsync(null);

        // Assert
        _cityRepositoryMock.Verify(repository => repository.GetCitiesAsync(null), Times.Once);
    }

    private static City CreateCity(
        int cityId,
        string name,
        string country,
        string postOffice)
    {
        return new City(
            name: name,
            country: country,
            postOffice: postOffice)
        {
            CityId = cityId
        };
    }
}