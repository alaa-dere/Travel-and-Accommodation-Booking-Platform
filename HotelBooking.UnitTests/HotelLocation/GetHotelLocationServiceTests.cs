using HotelBooking.Application.Exceptions;
using HotelBooking.Application.HotelLocations;
using HotelBooking.Application.HotelLocations.Dtos;
using HotelBooking.Application.Interfaces;
using Moq;

namespace HotelBooking.UnitTests.HotelLocations;

public class GetHotelLocationServiceTests
{
    private readonly Mock<IHotelLocationRepository> _repositoryMock;
    private readonly GetHotelLocationService _service;

    public GetHotelLocationServiceTests()
    {
        _repositoryMock = new Mock<IHotelLocationRepository>();

        _service = new GetHotelLocationService(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetHotelLocationAsync_WhenHotelDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        const int hotelId = 10;

        _repositoryMock
            .Setup(repository => repository.GetHotelLocationAsync(hotelId))
            .ReturnsAsync((HotelLocationResponseDto?)null);

        // Act
        var action = async () =>
            await _service.GetHotelLocationAsync(hotelId);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);
    }

    [Fact]
    public async Task GetHotelLocationAsync_WhenHotelExists_ShouldReturnHotelLocation()
    {
        // Arrange
        const int hotelId = 10;

        var location = new HotelLocationResponseDto();

        _repositoryMock
            .Setup(repository => repository.GetHotelLocationAsync(hotelId))
            .ReturnsAsync(location);

        // Act
        var result = await _service.GetHotelLocationAsync(hotelId);

        // Assert
        Assert.Same(location, result);
    }

    [Fact]
    public async Task GetHotelLocationAsync_ShouldRequestCorrectHotelFromRepository()
    {
        // Arrange
        const int hotelId = 10;

        var location = new HotelLocationResponseDto();

        _repositoryMock
            .Setup(repository => repository.GetHotelLocationAsync(hotelId))
            .ReturnsAsync(location);

        // Act
        await _service.GetHotelLocationAsync(hotelId);

        // Assert
        _repositoryMock.Verify(repository => repository.GetHotelLocationAsync(hotelId), Times.Once);
    }

    [Fact]
    public async Task GetHotelLocationAsync_WhenHotelDoesNotExist_ShouldCallRepositoryOnlyOnce()
    {
        // Arrange
        const int hotelId = 10;

        _repositoryMock
            .Setup(repository => repository.GetHotelLocationAsync(hotelId))
            .ReturnsAsync((HotelLocationResponseDto?)null);

        // Act
        try
        {
            await _service.GetHotelLocationAsync(hotelId);
        }
        catch (NotFoundException)
        {
        }

        // Assert
        _repositoryMock.Verify(repository => repository.GetHotelLocationAsync(hotelId), Times.Once);
    }
}