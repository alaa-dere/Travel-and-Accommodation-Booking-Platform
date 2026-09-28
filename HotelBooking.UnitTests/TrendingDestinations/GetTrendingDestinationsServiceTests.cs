using HotelBooking.Application.Interfaces;
using HotelBooking.Application.TrendingDestinations;
using HotelBooking.Application.TrendingDestinations.Dtos;
using Moq;

namespace HotelBooking.UnitTests.TrendingDestinations;

public class GetTrendingDestinationsServiceTests
{
    private static readonly DateTimeOffset UtcNow = new(2026, 9, 28, 12, 0, 0, TimeSpan.Zero);
    private readonly Mock<ITrendingDestinationRepository> _repositoryMock;
    private readonly GetTrendingDestinationsService _service;

    public GetTrendingDestinationsServiceTests()
    {
        _repositoryMock = new Mock<ITrendingDestinationRepository>();

        _service = new GetTrendingDestinationsService(
            _repositoryMock.Object,
            new FixedTimeProvider(UtcNow));
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
        _repositoryMock
            .Setup(repository =>
                repository.GetTrendingDestinationsAsync(
                    UtcNow.UtcDateTime.AddDays(-30)))
            .ReturnsAsync(
                new List<TrendingDestinationResponseDto>());

        // Act
        await _service.GetTrendingDestinationsAsync();

        // Assert
        _repositoryMock.Verify(
            repository => repository.GetTrendingDestinationsAsync(
                UtcNow.UtcDateTime.AddDays(-30)),
            Times.Once);
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

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
