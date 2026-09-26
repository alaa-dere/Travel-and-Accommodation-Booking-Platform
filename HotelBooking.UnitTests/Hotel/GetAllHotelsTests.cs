using HotelBooking.Application.Hotels.Retrive;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using Moq;

namespace HotelBooking.UnitTests.Hotels.Retrieve;

public class GetAllHotelsTests
{
    private readonly Mock<IHotelRepository> _hotelRepositoryMock;
    private readonly GetAllHotels _service;

    public GetAllHotelsTests()
    {
        _hotelRepositoryMock = new Mock<IHotelRepository>();
        _service = new GetAllHotels(_hotelRepositoryMock.Object);
    }

    [Fact]
    public async Task GetAllHotelsAsync_WhenNoHotelsExist_ShouldReturnEmptyCollection()
    {
        // Arrange
        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelsAsync(null))
            .ReturnsAsync(new List<Hotel>());

        // Act
        var result = await _service.GetAllHotelsAsync(null);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllHotelsAsync_WhenHotelExists_ShouldMapHotelCorrectly()
    {
        // Arrange
        var hotel = CreateHotel(
            hotelId: 10,
            cityId: 5,
            name: "Test Hotel");

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelsAsync(null))
            .ReturnsAsync(new List<Hotel> { hotel });

        // Act
        var result = (await _service.GetAllHotelsAsync(null)).Single();

        // Assert
        Assert.Equal(hotel.HotelId, result.HotelId);
        Assert.Equal(hotel.CityId, result.CityId);
        Assert.Equal(hotel.Name, result.Name);
        Assert.Equal(hotel.OwnerName, result.OwnerName);
        Assert.Equal(hotel.Address, result.Address);
        Assert.Equal(hotel.Latitude, result.Latitude);
        Assert.Equal(hotel.Longitude, result.Longitude);
        Assert.Equal(hotel.HotelType, result.HotelType);
        Assert.Equal(hotel.Description, result.Description);
        Assert.Equal(hotel.History, result.History);
        Assert.Equal(hotel.IsActive, result.IsActive);
    }

    [Fact]
    public async Task GetAllHotelsAsync_WhenMultipleHotelsExist_ShouldReturnAllHotels()
    {
        // Arrange
        var firstHotel = CreateHotel(
            hotelId: 10,
            cityId: 5,
            name: "First Hotel");

        var secondHotel = CreateHotel(
            hotelId: 20,
            cityId: 6,
            name: "Second Hotel");

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelsAsync(null))
            .ReturnsAsync(new List<Hotel>
            {
                firstHotel,
                secondHotel
            });

        // Act
        var result = (await _service.GetAllHotelsAsync(null)).ToList();

        // Assert
        Assert.Equal(2, result.Count);

        Assert.Equal(firstHotel.HotelId, result[0].HotelId);
        Assert.Equal(firstHotel.Name, result[0].Name);

        Assert.Equal(secondHotel.HotelId, result[1].HotelId);
        Assert.Equal(secondHotel.Name, result[1].Name);
    }

    [Fact]
    public async Task GetAllHotelsAsync_WhenSearchIsProvided_ShouldPassSearchToRepository()
    {
        // Arrange
        const string search = "Grand";

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelsAsync(search))
            .ReturnsAsync(new List<Hotel>());

        // Act
        await _service.GetAllHotelsAsync(search);

        // Assert
        _hotelRepositoryMock.Verify(
            repository => repository.GetHotelsAsync(search),
            Times.Once);
    }

    [Fact]
    public async Task GetAllHotelsAsync_WhenSearchIsNull_ShouldPassNullToRepository()
    {
        // Arrange
        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelsAsync(null))
            .ReturnsAsync(new List<Hotel>());

        // Act
        await _service.GetAllHotelsAsync(null);

        // Assert
        _hotelRepositoryMock.Verify(
            repository => repository.GetHotelsAsync(null),
            Times.Once);
    }

    [Fact]
    public async Task GetAllHotelsAsync_ShouldMapHotelActiveStatusCorrectly()
    {
        // Arrange
        var activeHotel = CreateHotel(
            hotelId: 10,
            cityId: 5,
            name: "Active Hotel");

        activeHotel.ChangeStatus(true);

        var inactiveHotel = CreateHotel(
            hotelId: 20,
            cityId: 5,
            name: "Inactive Hotel");

        inactiveHotel.ChangeStatus(false);

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelsAsync(null))
            .ReturnsAsync(new List<Hotel>
            {
                activeHotel,
                inactiveHotel
            });

        // Act
        var result = (await _service.GetAllHotelsAsync(null)).ToList();

        // Assert
        Assert.True(result[0].IsActive);
        Assert.False(result[1].IsActive);
    }

    private static Hotel CreateHotel(
        int hotelId,
        int cityId,
        string name)
    {
        return new Hotel(
            name: name,
            ownerName: "Test Owner",
            address: "Test Address",
            latitude: 32.2211,
            longitude: 35.2544,
            hotelType: (HotelType)1,
            cityId: cityId,
            description: "Test Description",
            history: "Test History")
        {
            HotelId = hotelId
        };
    }
}