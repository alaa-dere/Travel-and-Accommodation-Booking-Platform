using HotelBooking.Application.Exceptions;
using HotelBooking.Application.HotelReviews;
using HotelBooking.Application.HotelReviews.Dtos;
using HotelBooking.Application.Interfaces;
using Moq;

namespace HotelBooking.UnitTests.HotelReviews;

public class GetHotelReviewsTests
{
    private readonly Mock<IHotelReviewRepository> _reviewRepositoryMock;
    private readonly Mock<IHotelRepository> _hotelRepositoryMock;
    private readonly GetHotelReviews _service;

    public GetHotelReviewsTests()
    {
        _reviewRepositoryMock = new Mock<IHotelReviewRepository>();
        _hotelRepositoryMock = new Mock<IHotelRepository>();

        _service = new GetHotelReviews(_reviewRepositoryMock.Object, _hotelRepositoryMock.Object);
    }

    [Fact]
    public async Task GetHotelReviewsAsync_WhenHotelIsNotActive_ShouldThrowNotFoundException()
    {
        // Arrange
        const int hotelId = 10;

        _hotelRepositoryMock
            .Setup(repository => repository.IsActiveHotelAsync(hotelId))
            .ReturnsAsync(false);

        // Act
        var action = async () => await _service.GetHotelReviewsAsync(hotelId);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);
    }

    [Fact]
    public async Task GetHotelReviewsAsync_WhenHotelIsNotActive_ShouldNotRequestReviews()
    {
        // Arrange
        const int hotelId = 10;

        _hotelRepositoryMock
            .Setup(repository => repository.IsActiveHotelAsync(hotelId))
            .ReturnsAsync(false);

        // Act
        try
        {
            await _service.GetHotelReviewsAsync(hotelId);
        }
        catch (NotFoundException)
        {
        }

        // Assert
        _reviewRepositoryMock.Verify(repository => repository.GetReviewsByHotelIdAsync( It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetHotelReviewsAsync_WhenHotelHasNoReviews_ShouldReturnNullRating()
    {
        // Arrange
        const int hotelId = 10;

        _hotelRepositoryMock
            .Setup(repository => repository.IsActiveHotelAsync(hotelId))
            .ReturnsAsync(true);

        _reviewRepositoryMock
            .Setup(repository => repository.GetReviewsByHotelIdAsync(hotelId))
            .ReturnsAsync(new List<ReviewResponseDto>());

        // Act
        var result = await _service.GetHotelReviewsAsync(hotelId);

        // Assert
        Assert.Null(result.Rating);
        Assert.Empty(result.Reviews);
    }

    [Fact]
    public async Task GetHotelReviewsAsync_WhenHotelHasOneReview_ShouldReturnItsRating()
    {
        // Arrange
        const int hotelId = 10;

        var reviews = new List<ReviewResponseDto>
        {
            new()
            {
                Rating = 4,
                Comment = "Very good",
                CreatedAt = DateTime.UtcNow
            }
        };

        _hotelRepositoryMock
            .Setup(repository => repository.IsActiveHotelAsync(hotelId))
            .ReturnsAsync(true);

        _reviewRepositoryMock
            .Setup(repository => repository.GetReviewsByHotelIdAsync(hotelId))
            .ReturnsAsync(reviews);

        // Act
        var result = await _service.GetHotelReviewsAsync(hotelId);

        // Assert
        Assert.Equal(4d, result.Rating);
    }

    [Fact]
    public async Task GetHotelReviewsAsync_WhenHotelHasMultipleReviews_ShouldCalculateAverageRating()
    {
        // Arrange
        const int hotelId = 10;

        var reviews = new List<ReviewResponseDto>
        {
            new()
            {
                Rating = 5,
                Comment = "Excellent",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Rating = 4,
                Comment = "Very good",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Rating = 3,
                Comment = "Good",
                CreatedAt = DateTime.UtcNow
            }
        };

        _hotelRepositoryMock
            .Setup(repository => repository.IsActiveHotelAsync(hotelId))
            .ReturnsAsync(true);

        _reviewRepositoryMock
            .Setup(repository => repository.GetReviewsByHotelIdAsync(hotelId))
            .ReturnsAsync(reviews);

        // Act
        var result = await _service.GetHotelReviewsAsync(hotelId);

        // Assert
        Assert.Equal(4d, result.Rating);
    }

    [Fact]
    public async Task GetHotelReviewsAsync_ShouldCalculateFractionalAverageCorrectly()
    {
        // Arrange
        const int hotelId = 10;

        var reviews = new List<ReviewResponseDto>
        {
            new() { Rating = 5 },
            new() { Rating = 4 }
        };

        _hotelRepositoryMock
            .Setup(repository => repository.IsActiveHotelAsync(hotelId))
            .ReturnsAsync(true);

        _reviewRepositoryMock
            .Setup(repository => repository.GetReviewsByHotelIdAsync(hotelId))
            .ReturnsAsync(reviews);

        // Act
        var result = await _service.GetHotelReviewsAsync(hotelId);

        // Assert
        Assert.Equal(4.5d, result.Rating);
    }

    [Fact]
    public async Task GetHotelReviewsAsync_ShouldReturnReviewsFromRepository()
    {
        // Arrange
        const int hotelId = 10;

        var reviews = new List<ReviewResponseDto>
        {
            new()
            {
                Rating = 5,
                Comment = "Excellent",
                CreatedAt = new DateTime(2026, 9, 1)
            },
            new()
            {
                Rating = 3,
                Comment = "Good",
                CreatedAt = new DateTime(2026, 9, 2)
            }
        };

        _hotelRepositoryMock
            .Setup(repository => repository.IsActiveHotelAsync(hotelId))
            .ReturnsAsync(true);

        _reviewRepositoryMock
            .Setup(repository => repository.GetReviewsByHotelIdAsync(hotelId))
            .ReturnsAsync(reviews);

        // Act
        var result = await _service.GetHotelReviewsAsync(hotelId);

        // Assert
        Assert.Same(reviews, result.Reviews);
    }

    [Fact]
    public async Task GetHotelReviewsAsync_ShouldCheckCorrectHotel()
    {
        // Arrange
        const int hotelId = 10;

        _hotelRepositoryMock
            .Setup(repository => repository.IsActiveHotelAsync(hotelId))
            .ReturnsAsync(true);

        _reviewRepositoryMock
            .Setup(repository => repository.GetReviewsByHotelIdAsync(hotelId))
            .ReturnsAsync(new List<ReviewResponseDto>());

        // Act
        await _service.GetHotelReviewsAsync(hotelId);

        // Assert
        _hotelRepositoryMock.Verify(repository => repository.IsActiveHotelAsync(hotelId), Times.Once);
    }

    [Fact]
    public async Task GetHotelReviewsAsync_WhenHotelIsActive_ShouldRequestReviewsForCorrectHotel()
    {
        // Arrange
        const int hotelId = 10;

        _hotelRepositoryMock
            .Setup(repository => repository.IsActiveHotelAsync(hotelId))
            .ReturnsAsync(true);

        _reviewRepositoryMock
            .Setup(repository => repository.GetReviewsByHotelIdAsync(hotelId))
            .ReturnsAsync(new List<ReviewResponseDto>());

        // Act
        await _service.GetHotelReviewsAsync(hotelId);

        // Assert
        _reviewRepositoryMock.Verify(repository => repository.GetReviewsByHotelIdAsync(hotelId), Times.Once);
    }
}