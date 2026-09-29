using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.NearbyAttractions.GetByHotel;
using HotelBooking.Domain.Entities;
using Moq;

namespace HotelBooking.UnitTests.NearbyAttractions.GetByHotel;

public class GetNearbyAttractionsServiceTests
{
    private readonly Mock<INearbyAttractionRepository> _attractionRepositoryMock;
    private readonly Mock<IHotelRepository> _hotelRepositoryMock;
    private readonly GetNearbyAttractionsService _service;

    public GetNearbyAttractionsServiceTests()
    {
        _attractionRepositoryMock = new Mock<INearbyAttractionRepository>();
        _hotelRepositoryMock = new Mock<IHotelRepository>();

        _service = new GetNearbyAttractionsService(
            _attractionRepositoryMock.Object,
            _hotelRepositoryMock.Object);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByHotelIdAsync_WhenHotelIdIsInvalid_ShouldThrowBadRequestException(
        int hotelId)
    {
        // Act
        var action = async () =>
            await _service.GetByHotelIdAsync(hotelId);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        _hotelRepositoryMock.Verify(
            repository => repository.GetHotelByIdAsync(It.IsAny<int>()),
            Times.Never);

        _attractionRepositoryMock.Verify(
            repository => repository.GetByHotelIdAsync(It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByHotelIdAsync_WhenHotelDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        const int hotelId = 10;

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(hotelId))
            .ReturnsAsync((Hotel?)null);

        // Act
        var action = async () =>
            await _service.GetByHotelIdAsync(hotelId);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);

        _attractionRepositoryMock.Verify(
            repository => repository.GetByHotelIdAsync(It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByHotelIdAsync_WhenNoAttractionsExist_ShouldReturnEmptyCollection()
    {
        // Arrange
        const int hotelId = 10;

        var hotel = CreateHotel(hotelId);

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(hotelId))
            .ReturnsAsync(hotel);

        _attractionRepositoryMock
            .Setup(repository =>
                repository.GetByHotelIdAsync(hotelId))
            .ReturnsAsync(new List<NearbyAttraction>());

        // Act
        var result = await _service.GetByHotelIdAsync(hotelId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByHotelIdAsync_WhenAttractionExists_ShouldMapCorrectly()
    {
        // Arrange
        const int hotelId = 10;

        var hotel = CreateHotel(hotelId);

        var attraction = new NearbyAttraction(
            hotelId: hotelId,
            name: "Old City",
            description: "Historic area",
            latitude: 32.2211,
            longitude: 35.2544)
        {
            NearbyAttractionId = 100
        };

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(hotelId))
            .ReturnsAsync(hotel);

        _attractionRepositoryMock
            .Setup(repository =>
                repository.GetByHotelIdAsync(hotelId))
            .ReturnsAsync(new List<NearbyAttraction>
            {
                attraction
            });

        // Act
        var result = (await _service.GetByHotelIdAsync(hotelId)).Single();

        // Assert
        Assert.Equal(
            attraction.NearbyAttractionId,
            result.NearbyAttractionId);

        Assert.Equal(
            attraction.HotelId,
            result.HotelId);

        Assert.Equal(
            attraction.Name,
            result.Name);

        Assert.Equal(
            attraction.Description,
            result.Description);

        Assert.Equal(
            attraction.Latitude,
            result.Latitude);

        Assert.Equal(
            attraction.Longitude,
            result.Longitude);
    }

    [Fact]
    public async Task GetByHotelIdAsync_WhenMultipleAttractionsExist_ShouldReturnAll()
    {
        // Arrange
        const int hotelId = 10;

        var hotel = CreateHotel(hotelId);

        var firstAttraction = new NearbyAttraction(
            hotelId,
            "Old City",
            "Historic area",
            32.2211,
            35.2544)
        {
            NearbyAttractionId = 100
        };

        var secondAttraction = new NearbyAttraction(
            hotelId,
            "Museum",
            "Local museum",
            32.2200,
            35.2500)
        {
            NearbyAttractionId = 200
        };

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(hotelId))
            .ReturnsAsync(hotel);

        _attractionRepositoryMock
            .Setup(repository =>
                repository.GetByHotelIdAsync(hotelId))
            .ReturnsAsync(new List<NearbyAttraction>
            {
                firstAttraction,
                secondAttraction
            });

        // Act
        var result =
            (await _service.GetByHotelIdAsync(hotelId)).ToList();

        // Assert
        Assert.Equal(2, result.Count);

        Assert.Equal(
            firstAttraction.NearbyAttractionId,
            result[0].NearbyAttractionId);

        Assert.Equal(
            secondAttraction.NearbyAttractionId,
            result[1].NearbyAttractionId);
    }

    [Fact]
    public async Task GetByHotelIdAsync_ShouldRequestCorrectHotel()
    {
        // Arrange
        const int hotelId = 10;

        var hotel = CreateHotel(hotelId);

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(hotelId))
            .ReturnsAsync(hotel);

        _attractionRepositoryMock
            .Setup(repository =>
                repository.GetByHotelIdAsync(hotelId))
            .ReturnsAsync(new List<NearbyAttraction>());

        // Act
        await _service.GetByHotelIdAsync(hotelId);

        // Assert
        _hotelRepositoryMock.Verify(
            repository => repository.GetHotelByIdAsync(hotelId),
            Times.Once);
    }

    [Fact]
    public async Task GetByHotelIdAsync_WhenHotelExists_ShouldRequestAttractionsForCorrectHotel()
    {
        // Arrange
        const int hotelId = 10;

        var hotel = CreateHotel(hotelId);

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(hotelId))
            .ReturnsAsync(hotel);

        _attractionRepositoryMock
            .Setup(repository =>
                repository.GetByHotelIdAsync(hotelId))
            .ReturnsAsync(new List<NearbyAttraction>());

        // Act
        await _service.GetByHotelIdAsync(hotelId);

        // Assert
        _attractionRepositoryMock.Verify(
            repository => repository.GetByHotelIdAsync(hotelId),
            Times.Once);
    }

    private static Hotel CreateHotel(int hotelId)
    {
        return new Hotel(
            name: "Test Hotel",
            ownerName: "Test Owner",
            address: "Test Address",
            latitude: 32.2211,
            longitude: 35.2544,
            hotelType: (HotelType)1,
            cityId: 1,
            description: null,
            history: null)
        {
            HotelId = hotelId
        };
    }
}