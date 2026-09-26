using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.NearbyAttractions.Create;
using HotelBooking.Application.NearbyAttractions.Dtos;
using HotelBooking.Domain.Entities;
using Moq;

namespace HotelBooking.UnitTests.NearbyAttractions.Create;

public class CreateNearbyAttractionServiceTests
{
    private readonly Mock<INearbyAttractionRepository> _attractionRepositoryMock;
    private readonly Mock<IHotelRepository> _hotelRepositoryMock;
    private readonly CreateNearbyAttractionService _service;

    public CreateNearbyAttractionServiceTests()
    {
        _attractionRepositoryMock = new Mock<INearbyAttractionRepository>();
        _hotelRepositoryMock = new Mock<IHotelRepository>();

        _service = new CreateNearbyAttractionService(
            _attractionRepositoryMock.Object,
            _hotelRepositoryMock.Object);
    }

    [Fact]
    public async Task CreateAsync_WhenHotelIdIsInvalid_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.HotelId = 0;

        // Act
        var action = async () =>
            await _service.CreateAsync(request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        _hotelRepositoryMock.Verify(
            repository => repository.GetHotelByIdAsync(It.IsAny<int>()),
            Times.Never);

        VerifyAttractionWasNotSaved();
    }

    [Fact]
    public async Task CreateAsync_WhenNameIsEmpty_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Name = string.Empty;

        // Act
        var action = async () =>
            await _service.CreateAsync(request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        _hotelRepositoryMock.Verify(
            repository => repository.GetHotelByIdAsync(It.IsAny<int>()),
            Times.Never);

        VerifyAttractionWasNotSaved();
    }

    [Fact]
    public async Task CreateAsync_WhenNameIsWhitespace_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Name = "   ";

        // Act
        var action = async () =>
            await _service.CreateAsync(request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        VerifyAttractionWasNotSaved();
    }

    [Fact]
    public async Task CreateAsync_WhenNameExceeds100Characters_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Name = new string('A', 101);

        // Act
        var action = async () =>
            await _service.CreateAsync(request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        VerifyAttractionWasNotSaved();
    }

    [Fact]
    public async Task CreateAsync_WhenDescriptionExceeds500Characters_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Description = new string('A', 501);

        // Act
        var action = async () =>
            await _service.CreateAsync(request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        VerifyAttractionWasNotSaved();
    }

    [Theory]
    [InlineData(-90.1)]
    [InlineData(90.1)]
    public async Task CreateAsync_WhenLatitudeIsOutsideValidRange_ShouldThrowBadRequestException(
        double latitude)
    {
        // Arrange
        var request = CreateValidRequest();
        request.Latitude = latitude;

        // Act
        var action = async () =>
            await _service.CreateAsync(request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        VerifyAttractionWasNotSaved();
    }

    [Theory]
    [InlineData(-180.1)]
    [InlineData(180.1)]
    public async Task CreateAsync_WhenLongitudeIsOutsideValidRange_ShouldThrowBadRequestException(
        double longitude)
    {
        // Arrange
        var request = CreateValidRequest();
        request.Longitude = longitude;

        // Act
        var action = async () =>
            await _service.CreateAsync(request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        VerifyAttractionWasNotSaved();
    }

    [Fact]
    public async Task CreateAsync_WhenHotelDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        var request = CreateValidRequest();

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(request.HotelId))
            .ReturnsAsync((Hotel?)null);

        // Act
        var action = async () =>
            await _service.CreateAsync(request);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);

        VerifyAttractionWasNotSaved();
    }

    [Fact]
    public async Task CreateAsync_WhenHotelDoesNotExist_ShouldNotSaveAttraction()
    {
        // Arrange
        var request = CreateValidRequest();

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(request.HotelId))
            .ReturnsAsync((Hotel?)null);

        // Act
        try
        {
            await _service.CreateAsync(request);
        }
        catch (NotFoundException)
        {
        }

        // Assert
        VerifyAttractionWasNotSaved();
    }

    [Fact]
    public async Task CreateAsync_WhenRequestIsValid_ShouldCheckCorrectHotel()
    {
        // Arrange
        var request = CreateValidRequest();
        var hotel = CreateHotel(request.HotelId);

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(request.HotelId))
            .ReturnsAsync(hotel);

        // Act
        await _service.CreateAsync(request);

        // Assert
        _hotelRepositoryMock.Verify(
            repository =>
                repository.GetHotelByIdAsync(request.HotelId),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenRequestIsValid_ShouldAddAttractionWithCorrectData()
    {
        // Arrange
        var request = CreateValidRequest();
        var hotel = CreateHotel(request.HotelId);

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(request.HotelId))
            .ReturnsAsync(hotel);

        NearbyAttraction? addedAttraction = null;

        _attractionRepositoryMock
            .Setup(repository =>
                repository.AddAsync(It.IsAny<NearbyAttraction>()))
            .Callback<NearbyAttraction>(attraction =>
                addedAttraction = attraction);

        // Act
        await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(addedAttraction);

        Assert.Equal(
            request.HotelId,
            addedAttraction!.HotelId);

        Assert.Equal(
            request.Name,
            addedAttraction.Name);

        Assert.Equal(
            request.Description,
            addedAttraction.Description);

        Assert.Equal(
            request.Latitude,
            addedAttraction.Latitude);

        Assert.Equal(
            request.Longitude,
            addedAttraction.Longitude);
    }

    [Fact]
    public async Task CreateAsync_WhenRequestIsValid_ShouldAddAttractionOnce()
    {
        // Arrange
        var request = CreateValidRequest();
        var hotel = CreateHotel(request.HotelId);

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(request.HotelId))
            .ReturnsAsync(hotel);

        // Act
        await _service.CreateAsync(request);

        // Assert
        _attractionRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.Is<NearbyAttraction>(attraction =>
                    attraction.HotelId == request.HotelId &&
                    attraction.Name == request.Name &&
                    attraction.Description == request.Description &&
                    attraction.Latitude == request.Latitude &&
                    attraction.Longitude == request.Longitude)),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenRequestIsValid_ShouldSaveChangesOnce()
    {
        // Arrange
        var request = CreateValidRequest();
        var hotel = CreateHotel(request.HotelId);

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(request.HotelId))
            .ReturnsAsync(hotel);

        // Act
        await _service.CreateAsync(request);

        // Assert
        _attractionRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenCoordinatesAreAtBoundary_ShouldCreateAttraction()
    {
        // Arrange
        var request = CreateValidRequest();

        request.Latitude = 90;
        request.Longitude = 180;

        var hotel = CreateHotel(request.HotelId);

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(request.HotelId))
            .ReturnsAsync(hotel);

        // Act
        await _service.CreateAsync(request);

        // Assert
        _attractionRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.Is<NearbyAttraction>(attraction =>
                    attraction.Latitude == 90 &&
                    attraction.Longitude == 180)),
            Times.Once);

        _attractionRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once);
    }

    private void VerifyAttractionWasNotSaved()
    {
        _attractionRepositoryMock.Verify(
            repository =>
                repository.AddAsync(
                    It.IsAny<NearbyAttraction>()),
            Times.Never);

        _attractionRepositoryMock.Verify(
            repository =>
                repository.SaveChangesAsync(),
            Times.Never);
    }

    private static CreateNearbyAttractionRequestDto CreateValidRequest()
    {
        return new CreateNearbyAttractionRequestDto
        {
            HotelId = 10,
            Name = "Old City",
            Description = "Historic area near the hotel.",
            Latitude = 32.2211,
            Longitude = 35.2544
        };
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