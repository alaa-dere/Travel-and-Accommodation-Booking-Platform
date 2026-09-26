using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Hotels.Create;
using HotelBooking.Application.Hotels.Dtos;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using Moq;

namespace HotelBooking.UnitTests.Hotels.Create;

public class CreateHotelTests
{
    private readonly Mock<IHotelRepository> _hotelRepositoryMock;
    private readonly Mock<ICityRepository> _cityRepositoryMock;
    private readonly CreateHotel _service;

    public CreateHotelTests()
    {
        _hotelRepositoryMock = new Mock<IHotelRepository>();
        _cityRepositoryMock = new Mock<ICityRepository>();

        _service = new CreateHotel(
            _hotelRepositoryMock.Object,
            _cityRepositoryMock.Object);
    }

    [Fact]
    public async Task CreateHotelAsync_WhenCityDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        var request = CreateValidRequest();

        _cityRepositoryMock
            .Setup(repository =>
                repository.GetCityByIdAsync(request.CityId))
            .ReturnsAsync((City?)null);

        // Act
        var action = async () =>
            await _service.CreateHotelAsync(request);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);
    }

    [Fact]
    public async Task CreateHotelAsync_WhenCityDoesNotExist_ShouldNotAddHotel()
    {
        // Arrange
        var request = CreateValidRequest();

        _cityRepositoryMock
            .Setup(repository =>
                repository.GetCityByIdAsync(request.CityId))
            .ReturnsAsync((City?)null);

        // Act
        try
        {
            await _service.CreateHotelAsync(request);
        }
        catch (NotFoundException)
        {
        }

        // Assert
        _hotelRepositoryMock.Verify(
            repository => repository.Add(
                It.IsAny<Hotel>()),
            Times.Never);

        _hotelRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task CreateHotelAsync_ShouldRequestCorrectCity()
    {
        // Arrange
        var request = CreateValidRequest();
        var city = CreateCity(request.CityId);

        _cityRepositoryMock
            .Setup(repository =>
                repository.GetCityByIdAsync(request.CityId))
            .ReturnsAsync(city);

        // Act
        await _service.CreateHotelAsync(request);

        // Assert
        _cityRepositoryMock.Verify(
            repository =>
                repository.GetCityByIdAsync(request.CityId),
            Times.Once);
    }

    [Fact]
    public async Task CreateHotelAsync_WhenRequestIsValid_ShouldAddHotelWithCorrectData()
    {
        // Arrange
        var request = CreateValidRequest();
        var city = CreateCity(request.CityId);

        _cityRepositoryMock
            .Setup(repository =>
                repository.GetCityByIdAsync(request.CityId))
            .ReturnsAsync(city);

        Hotel? addedHotel = null;

        _hotelRepositoryMock
            .Setup(repository =>
                repository.Add(It.IsAny<Hotel>()))
            .Callback<Hotel>(hotel =>
                addedHotel = hotel);

        // Act
        await _service.CreateHotelAsync(request);

        // Assert
        Assert.NotNull(addedHotel);

        Assert.Equal(request.Name, addedHotel!.Name);
        Assert.Equal(request.OwnerName, addedHotel.OwnerName);
        Assert.Equal(request.Address, addedHotel.Address);
        Assert.Equal(request.Latitude, addedHotel.Latitude);
        Assert.Equal(request.Longitude, addedHotel.Longitude);
        Assert.Equal(request.HotelType, addedHotel.HotelType);
        Assert.Equal(request.CityId, addedHotel.CityId);
        Assert.Equal(request.Description, addedHotel.Description);
        Assert.Equal(request.History, addedHotel.History);

        _hotelRepositoryMock.Verify(
            repository => repository.Add(
                It.IsAny<Hotel>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateHotelAsync_WhenRequestIsValid_ShouldSaveChangesOnce()
    {
        // Arrange
        var request = CreateValidRequest();
        var city = CreateCity(request.CityId);

        _cityRepositoryMock
            .Setup(repository =>
                repository.GetCityByIdAsync(request.CityId))
            .ReturnsAsync(city);

        // Act
        await _service.CreateHotelAsync(request);

        // Assert
        _hotelRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task CreateHotelAsync_WhenRequestIsValid_ShouldReturnMappedResponse()
    {
        // Arrange
        var request = CreateValidRequest();
        var city = CreateCity(request.CityId);

        _cityRepositoryMock
            .Setup(repository =>
                repository.GetCityByIdAsync(request.CityId))
            .ReturnsAsync(city);

        _hotelRepositoryMock
            .Setup(repository =>
                repository.Add(It.IsAny<Hotel>()))
            .Callback<Hotel>(hotel =>
            {
                hotel.HotelId = 100;
            });

        // Act
        var result = await _service.CreateHotelAsync(request);

        // Assert
        Assert.Equal(100, result.HotelId);
        Assert.Equal(request.CityId, result.CityId);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.OwnerName, result.OwnerName);
        Assert.Equal(request.HotelType, result.HotelType);
        Assert.Equal(request.Address, result.Address);
        Assert.Equal(request.Latitude, result.Latitude);
        Assert.Equal(request.Longitude, result.Longitude);
        Assert.Equal(request.Description, result.Description);
        Assert.Equal(request.History, result.History);
    }

    [Fact]
    public async Task CreateHotelAsync_WhenRequestIsValid_ShouldReturnHotelActiveStatus()
    {
        // Arrange
        var request = CreateValidRequest();
        var city = CreateCity(request.CityId);

        _cityRepositoryMock
            .Setup(repository =>
                repository.GetCityByIdAsync(request.CityId))
            .ReturnsAsync(city);

        // Act
        var result = await _service.CreateHotelAsync(request);

        // Assert
        Hotel? addedHotel = null;

        _hotelRepositoryMock.Verify(
            repository => repository.Add(
                It.Is<Hotel>(hotel =>
                    hotel.IsActive == result.IsActive)),
            Times.Once);
    }

    private static City CreateCity(int cityId)
    {
        return new City(
            name: "Nablus",
            country: "Palestine",
            postOffice: "P400")
        {
            CityId = cityId
        };
    }

    private static HotelRequestDto CreateValidRequest()
    {
        return new HotelRequestDto
        {
            Name = "Test Hotel",
            OwnerName = "Test Owner",
            Address = "Test Address",
            Latitude = 32.2211,
            Longitude = 35.2544,
            HotelType = (HotelType)1,
            CityId = 10,
            Description = "Test Description",
            History = "Test History"
        };
    }
}