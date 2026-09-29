using HotelBooking.Application.Interfaces;
using HotelBooking.Application.RecentlyVisitedHotels;
using HotelBooking.Application.RecentlyVisitedHotels.Dtos;
using Moq;

namespace HotelBooking.UnitTests.RecentlyVisitedHotels;

public class GetRecentlyVisitedHotelsServiceTests
{
    private readonly Mock<IRecentlyVisitedHotelRepository> _repositoryMock;
    private readonly GetRecentlyVisitedHotelsService _service;

    public GetRecentlyVisitedHotelsServiceTests()
    {
        _repositoryMock = new Mock<IRecentlyVisitedHotelRepository>();

        _service = new GetRecentlyVisitedHotelsService(
            _repositoryMock.Object);
    }

    [Fact]
    public async Task GetRecentlyVisitedHotelsAsync_WhenNoHotelsExist_ShouldReturnEmptyList()
    {
        // Arrange
        const int userId = 10;

        var hotels = new List<RecentlyVisitedHotelResponseDto>();

        _repositoryMock
            .Setup(repository =>
                repository.GetRecentlyVisitedHotelsAsync(userId))
            .ReturnsAsync(hotels);

        // Act
        var result =
            await _service.GetRecentlyVisitedHotelsAsync(userId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetRecentlyVisitedHotelsAsync_ShouldReturnRepositoryResult()
    {
        // Arrange
        const int userId = 10;

        var hotels = new List<RecentlyVisitedHotelResponseDto>
        {
            new(),
            new()
        };

        _repositoryMock
            .Setup(repository =>
                repository.GetRecentlyVisitedHotelsAsync(userId))
            .ReturnsAsync(hotels);

        // Act
        var result =
            await _service.GetRecentlyVisitedHotelsAsync(userId);

        // Assert
        Assert.Same(hotels, result);
    }

    [Fact]
    public async Task GetRecentlyVisitedHotelsAsync_ShouldPassCorrectUserIdToRepository()
    {
        // Arrange
        const int userId = 25;

        _repositoryMock
            .Setup(repository =>
                repository.GetRecentlyVisitedHotelsAsync(userId))
            .ReturnsAsync(new List<RecentlyVisitedHotelResponseDto>());

        // Act
        await _service.GetRecentlyVisitedHotelsAsync(userId);

        // Assert
        _repositoryMock.Verify(
            repository =>
                repository.GetRecentlyVisitedHotelsAsync(userId),
            Times.Once);
    }

    [Fact]
    public async Task GetRecentlyVisitedHotelsAsync_ShouldCallRepositoryOnlyOnce()
    {
        // Arrange
        const int userId = 10;

        _repositoryMock
            .Setup(repository =>
                repository.GetRecentlyVisitedHotelsAsync(userId))
            .ReturnsAsync(new List<RecentlyVisitedHotelResponseDto>());

        // Act
        await _service.GetRecentlyVisitedHotelsAsync(userId);

        // Assert
        _repositoryMock.Verify(
            repository =>
                repository.GetRecentlyVisitedHotelsAsync(
                    It.IsAny<int>()),
            Times.Once);
    }
}