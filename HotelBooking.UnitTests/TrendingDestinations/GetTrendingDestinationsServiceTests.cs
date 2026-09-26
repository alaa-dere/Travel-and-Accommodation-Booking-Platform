using HotelBooking.Application.Interfaces;
using HotelBooking.Application.TrendingDestinations;
using HotelBooking.Application.TrendingDestinations.Dtos;
using Moq;

namespace HotelBooking.UnitTests.TrendingDestinations;

public class GetTrendingDestinationsServiceTests
{
    private readonly Mock<ITrendingDestinationRepository> _repositoryMock;
    private readonly GetTrendingDestinationsService _service;

    public GetTrendingDestinationsServiceTests()
    {
        _repositoryMock = new Mock<ITrendingDestinationRepository>();

        _service = new GetTrendingDestinationsService(
            _repositoryMock.Object);
    }

    [Fact]
    public async Task GetTrendingDestinationsAsync_WhenNoDestinationsExist_ShouldReturnEmptyList()
    {
        // Arrange
        _repositoryMock
            .Setup(repository =>
                repository.GetTrendingDestinationsAsync(
                    It.IsAny<DateTime>()))
            .ReturnsAsync(new List<TrendingDestinationResponseDto>());

        // Act
        var result =
            await _service.GetTrendingDestinationsAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetTrendingDestinationsAsync_ShouldReturnRepositoryResult()
    {
        // Arrange
        var destinations =
            new List<TrendingDestinationResponseDto>
            {
                new(),
                new()
            };

        _repositoryMock
            .Setup(repository =>
                repository.GetTrendingDestinationsAsync(
                    It.IsAny<DateTime>()))
            .ReturnsAsync(destinations);

        // Act
        var result =
            await _service.GetTrendingDestinationsAsync();

        // Assert
        Assert.Same(destinations, result);
    }

    [Fact]
    public async Task GetTrendingDestinationsAsync_ShouldUseDateThirtyDaysAgo()
    {
        // Arrange
        DateTime? capturedFromDate = null;

        _repositoryMock
            .Setup(repository =>
                repository.GetTrendingDestinationsAsync(
                    It.IsAny<DateTime>()))
            .Callback<DateTime>(fromDate =>
                capturedFromDate = fromDate)
            .ReturnsAsync(
                new List<TrendingDestinationResponseDto>());

        var beforeCall = DateTime.UtcNow.AddDays(-30);

        // Act
        await _service.GetTrendingDestinationsAsync();

        var afterCall = DateTime.UtcNow.AddDays(-30);

        // Assert
        Assert.NotNull(capturedFromDate);

        Assert.True(
            capturedFromDate >= beforeCall);

        Assert.True(
            capturedFromDate <= afterCall);
    }

    [Fact]
    public async Task GetTrendingDestinationsAsync_ShouldCallRepositoryOnce()
    {
        // Arrange
        _repositoryMock
            .Setup(repository =>
                repository.GetTrendingDestinationsAsync(
                    It.IsAny<DateTime>()))
            .ReturnsAsync(
                new List<TrendingDestinationResponseDto>());

        // Act
        await _service.GetTrendingDestinationsAsync();

        // Assert
        _repositoryMock.Verify(
            repository =>
                repository.GetTrendingDestinationsAsync(
                    It.IsAny<DateTime>()),
            Times.Once);
    }
}