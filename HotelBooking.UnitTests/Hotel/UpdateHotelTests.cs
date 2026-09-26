using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Hotels.Dtos;
using HotelBooking.Application.Hotels.Update;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using Moq;

namespace HotelBooking.UnitTests.Hotels.Update;

public class UpdateHotelTests
{
    private readonly Mock<IHotelRepository> _hotelRepositoryMock;
    private readonly Mock<ICityRepository> _cityRepositoryMock;
    private readonly UpdateHotel _service;

    public UpdateHotelTests()
    {
        _hotelRepositoryMock = new Mock<IHotelRepository>();
        _cityRepositoryMock = new Mock<ICityRepository>();

        _service = new UpdateHotel(
            _hotelRepositoryMock.Object,
            _cityRepositoryMock.Object);
    }

    [Fact]
    public async Task UpdateHotelAsync_WhenHotelDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        const int hotelId = 10;
        var request = CreateValidRequest();

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(hotelId))
            .ReturnsAsync((Hotel?)null);

        // Act
        var action = async () =>
            await _service.UpdateHotelAsync(hotelId, request);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);

        _cityRepositoryMock.Verify(
            repository => repository.GetCityByIdAsync(It.IsAny<int>()),
            Times.Never);

        _hotelRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task UpdateHotelAsync_WhenCityDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        const int hotelId = 10;

        var hotel = CreateHotel(hotelId);
        var request = CreateValidRequest();

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(hotelId))
            .ReturnsAsync(hotel);

        _cityRepositoryMock
            .Setup(repository =>
                repository.GetCityByIdAsync(request.CityId))
            .ReturnsAsync((City?)null);

        // Act
        var action = async () =>
            await _service.UpdateHotelAsync(hotelId, request);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);

        _hotelRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task UpdateHotelAsync_WhenRequestIsValid_ShouldUpdateHotelFields()
    {
        // Arrange
        const int hotelId = 10;

        var hotel = CreateHotel(hotelId);
        var request = CreateValidRequest();
        var city = CreateCity(request.CityId);

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(hotelId))
            .ReturnsAsync(hotel);

        _cityRepositoryMock
            .Setup(repository =>
                repository.GetCityByIdAsync(request.CityId))
            .ReturnsAsync(city);

        // Act
        await _service.UpdateHotelAsync(hotelId, request);

        // Assert
        Assert.Equal(request.Name, hotel.Name);
        Assert.Equal(request.OwnerName, hotel.OwnerName);
        Assert.Equal(request.Address, hotel.Address);
        Assert.Equal(request.Latitude, hotel.Latitude);
        Assert.Equal(request.Longitude, hotel.Longitude);
        Assert.Equal(request.HotelType, hotel.HotelType);
        Assert.Equal(request.CityId, hotel.CityId);
        Assert.Equal(request.Description, hotel.Description);
        Assert.Equal(request.History, hotel.History);
        Assert.NotNull(hotel.UpdatedAt);
    }

    [Fact]
    public async Task UpdateHotelAsync_WhenRequestIsValid_ShouldSaveChangesOnce()
    {
        // Arrange
        const int hotelId = 10;

        var hotel = CreateHotel(hotelId);
        var request = CreateValidRequest();
        var city = CreateCity(request.CityId);

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(hotelId))
            .ReturnsAsync(hotel);

        _cityRepositoryMock
            .Setup(repository =>
                repository.GetCityByIdAsync(request.CityId))
            .ReturnsAsync(city);

        // Act
        await _service.UpdateHotelAsync(hotelId, request);

        // Assert
        _hotelRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task UpdateHotelAsync_WhenRequestIsValid_ShouldReturnMappedResponse()
    {
        // Arrange
        const int hotelId = 10;

        var hotel = CreateHotel(hotelId);
        var request = CreateValidRequest();
        var city = CreateCity(request.CityId);

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(hotelId))
            .ReturnsAsync(hotel);

        _cityRepositoryMock
            .Setup(repository =>
                repository.GetCityByIdAsync(request.CityId))
            .ReturnsAsync(city);

        // Act
        var result = await _service.UpdateHotelAsync(
            hotelId,
            request);

        // Assert
        Assert.Equal(hotelId, result.HotelId);
        Assert.Equal(request.CityId, result.CityId);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.OwnerName, result.OwnerName);
        Assert.Equal(request.Address, result.Address);
        Assert.Equal(request.Latitude, result.Latitude);
        Assert.Equal(request.Longitude, result.Longitude);
        Assert.Equal(request.HotelType, result.HotelType);
        Assert.Equal(request.Description, result.Description);
        Assert.Equal(request.History, result.History);
        Assert.Equal(hotel.IsActive, result.IsActive);
    }

    [Fact]
    public async Task UpdateHotelAsync_ShouldRequestCorrectHotel()
    {
        // Arrange
        const int hotelId = 10;

        var hotel = CreateHotel(hotelId);
        var request = CreateValidRequest();
        var city = CreateCity(request.CityId);

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(hotelId))
            .ReturnsAsync(hotel);

        _cityRepositoryMock
            .Setup(repository =>
                repository.GetCityByIdAsync(request.CityId))
            .ReturnsAsync(city);

        // Act
        await _service.UpdateHotelAsync(hotelId, request);

        // Assert
        _hotelRepositoryMock.Verify(
            repository => repository.GetHotelByIdAsync(hotelId),
            Times.Once);
    }

    [Fact]
    public async Task UpdateHotelAsync_ShouldRequestCorrectCity()
    {
        // Arrange
        const int hotelId = 10;

        var hotel = CreateHotel(hotelId);
        var request = CreateValidRequest();
        var city = CreateCity(request.CityId);

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(hotelId))
            .ReturnsAsync(hotel);

        _cityRepositoryMock
            .Setup(repository =>
                repository.GetCityByIdAsync(request.CityId))
            .ReturnsAsync(city);

        // Act
        await _service.UpdateHotelAsync(hotelId, request);

        // Assert
        _cityRepositoryMock.Verify(
            repository =>
                repository.GetCityByIdAsync(request.CityId),
            Times.Once);
    }

    private static Hotel CreateHotel(int hotelId)
    {
        return new Hotel(
            name: "Old Hotel",
            ownerName: "Old Owner",
            address: "Old Address",
            latitude: 31.0,
            longitude: 35.0,
            hotelType: (HotelType)1,
            cityId: 1,
            description: "Old Description",
            history: "Old History")
        {
            HotelId = hotelId
        };
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
            Name = "Updated Hotel",
            OwnerName = "Updated Owner",
            Address = "Updated Address",
            Latitude = 32.2211,
            Longitude = 35.2544,
            HotelType = (HotelType)1,
            CityId = 5,
            Description = "Updated Description",
            History = "Updated History"
        };
    }
}