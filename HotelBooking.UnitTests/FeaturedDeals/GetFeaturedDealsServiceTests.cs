using HotelBooking.Application.FeatureDeals;
using HotelBooking.Application.FeatureDeals.Models;
using HotelBooking.Application.Interfaces;
using Moq;

namespace HotelBooking.UnitTests.FeatureDeals;

public class GetFeaturedDealsServiceTests
{
    private static readonly DateTimeOffset UtcNow = new(2026, 9, 28, 12, 0, 0, TimeSpan.Zero);
    private readonly Mock<IFeaturedDealsRepository> _repositoryMock;
    private readonly GetFeaturedDealsService _service;

    public GetFeaturedDealsServiceTests()
    {
        _repositoryMock = new Mock<IFeaturedDealsRepository>();
        _service = new GetFeaturedDealsService(
            _repositoryMock.Object,
            new FixedTimeProvider(UtcNow));
    }

    [Fact]
    public async Task GetFeaturedDealsAsync_WhenNoDealsExist_ShouldReturnEmptyCollection()
    {
        // Arrange
        _repositoryMock
            .Setup(repository => repository.GetEligibleFeaturedDealsAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(new List<FeaturedDealData>());

        // Act
        var result = await _service.GetFeaturedDealsAsync();

        // Assert
        Assert.Empty(result);

        _repositoryMock.Verify(
            repository => repository.GetEligibleFeaturedDealsAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()),
            Times.Once);
    }

    [Fact]
    public async Task GetFeaturedDealsAsync_WhenDealExists_ShouldMapDealCorrectly()
    {
        // Arrange
        var deal = CreateDeal();

        _repositoryMock
            .Setup(repository => repository.GetEligibleFeaturedDealsAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(new List<FeaturedDealData> { deal });

        // Act
        var result = (await _service.GetFeaturedDealsAsync()).ToList();

        // Assert
        Assert.Single(result);

        var response = result[0];

        Assert.Equal(deal.HotelId, response.HotelId);
        Assert.Equal(deal.HotelName, response.HotelName);
        Assert.Equal(deal.City, response.City);
        Assert.Equal(deal.Address, response.Address);
        Assert.Equal(deal.ThumbnailUrl, response.ThumbnailUrl);
        Assert.Equal(deal.AverageRating, response.Rating);
        Assert.Equal(deal.StartingPrice, response.OriginalPrice);
        Assert.Equal(deal.DiscountPercentage, response.DiscountPercentage);
    }

    [Fact]
    public async Task GetFeaturedDealsAsync_ShouldCalculateDiscountedPriceCorrectly()
    {
        // Arrange
        var deal = CreateDeal();

        deal.StartingPrice = 200m;
        deal.DiscountPercentage = 25;

        _repositoryMock
            .Setup(repository => repository.GetEligibleFeaturedDealsAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(new List<FeaturedDealData> { deal });

        // Act
        var result = (await _service.GetFeaturedDealsAsync()).Single();

        // Assert
        Assert.Equal(200m, result.OriginalPrice);
        Assert.Equal(25, result.DiscountPercentage);
        Assert.Equal(150m, result.DiscountedPrice);
    }

    [Fact]
    public async Task GetFeaturedDealsAsync_WhenDiscountIsZero_ShouldReturnOriginalPriceAsDiscountedPrice()
    {
        // Arrange
        var deal = CreateDeal();

        deal.StartingPrice = 200m;
        deal.DiscountPercentage = 0;

        _repositoryMock
            .Setup(repository => repository.GetEligibleFeaturedDealsAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(new List<FeaturedDealData> { deal });

        // Act
        var result = (await _service.GetFeaturedDealsAsync()).Single();

        // Assert
        Assert.Equal(200m, result.DiscountedPrice);
    }

    [Fact]
    public async Task GetFeaturedDealsAsync_WhenMultipleDealsExist_ShouldReturnAllDeals()
    {
        // Arrange
        var firstDeal = CreateDeal();

        var secondDeal = new FeaturedDealData
        {
            HotelId = 2,
            HotelName = "Second Hotel",
            City = "Ramallah",
            Address = "Second Address",
            ThumbnailUrl = "second.jpg",
            AverageRating = 4.2m,
            StartingPrice = 300m,
            DiscountPercentage = 10,
            BookingCountLast30Days = 3
        };

        _repositoryMock
            .Setup(repository => repository.GetEligibleFeaturedDealsAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(new List<FeaturedDealData>
            {
                firstDeal,
                secondDeal
            });

        // Act
        var result = (await _service.GetFeaturedDealsAsync()).ToList();

        // Assert
        Assert.Equal(2, result.Count);

        Assert.Equal(firstDeal.HotelId, result[0].HotelId);
        Assert.Equal(secondDeal.HotelId, result[1].HotelId);

        Assert.Equal(150m, result[0].DiscountedPrice);
        Assert.Equal(270m, result[1].DiscountedPrice);
    }

    [Fact]
    public async Task GetFeaturedDealsAsync_ShouldRequestDealsForThirtyDayWindow()
    {
        // Arrange
        _repositoryMock
            .Setup(repository => repository.GetEligibleFeaturedDealsAsync(
                UtcNow.UtcDateTime,
                UtcNow.UtcDateTime.AddDays(-30)))
            .ReturnsAsync(new List<FeaturedDealData>());

        // Act
        await _service.GetFeaturedDealsAsync();

        // Assert
        _repositoryMock.Verify(
            repository => repository.GetEligibleFeaturedDealsAsync(
                UtcNow.UtcDateTime,
                UtcNow.UtcDateTime.AddDays(-30)),
            Times.Once);
    }

    [Fact]
    public async Task GetFeaturedDealsAsync_WhenRatingIsNull_ShouldMapNullRating()
    {
        // Arrange
        var deal = CreateDeal();
        deal.AverageRating = null;

        _repositoryMock
            .Setup(repository => repository.GetEligibleFeaturedDealsAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(new List<FeaturedDealData> { deal });

        // Act
        var result = (await _service.GetFeaturedDealsAsync()).Single();

        // Assert
        Assert.Null(result.Rating);
    }

    [Fact]
    public async Task GetFeaturedDealsAsync_WhenThumbnailIsNull_ShouldMapNullThumbnail()
    {
        // Arrange
        var deal = CreateDeal();
        deal.ThumbnailUrl = null;

        _repositoryMock
            .Setup(repository => repository.GetEligibleFeaturedDealsAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()))
            .ReturnsAsync(new List<FeaturedDealData> { deal });

        // Act
        var result = (await _service.GetFeaturedDealsAsync()).Single();

        // Assert
        Assert.Null(result.ThumbnailUrl);
    }

    private static FeaturedDealData CreateDeal()
    {
        return new FeaturedDealData
        {
            HotelId = 1,
            HotelName = "Test Hotel",
            City = "Nablus",
            Address = "Test Address",
            ThumbnailUrl = "hotel.jpg",
            AverageRating = 4.5m,
            StartingPrice = 200m,
            DiscountPercentage = 25,
            BookingCountLast30Days = 5
        };
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
