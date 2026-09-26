using HotelBooking.Domain.Entities;

namespace HotelBooking.UnitTests.Domain.Entities;

public class ReviewTests
{
    [Fact]
    public void Constructor_WhenDataIsValid_ShouldCreateReviewWithCorrectValues()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var review = new Review(
            bookingId: 1,
            rating: 5,
            comment: "Excellent hotel");

        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(1, review.BookingId);
        Assert.Equal(5, review.Rating);
        Assert.Equal("Excellent hotel", review.Comment);

        Assert.InRange(
            review.CreatedAt,
            beforeCreation,
            afterCreation);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenBookingIdIsInvalid_ShouldThrowArgumentException(
        int bookingId)
    {
        // Act
        var action = () =>
            new Review(
                bookingId,
                rating: 5,
                comment: "Excellent hotel");

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(10)]
    public void Constructor_WhenRatingIsOutsideValidRange_ShouldThrowArgumentOutOfRangeException(
        int rating)
    {
        // Act
        var action = () =>
            new Review(
                bookingId: 1,
                rating,
                comment: "Excellent hotel");

        // Assert
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(action);

        Assert.Equal("rating", exception.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void Constructor_WhenRatingIsAtValidBoundary_ShouldSucceed(
        int rating)
    {
        // Act
        var review = new Review(
            bookingId: 1,
            rating,
            comment: "Good hotel");

        // Assert
        Assert.Equal(rating, review.Rating);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenCommentIsInvalid_ShouldThrowArgumentException(
        string comment)
    {
        // Act
        var action = () =>
            new Review(
                bookingId: 1,
                rating: 5,
                comment);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Constructor_ShouldPreserveCommentAsProvided()
    {
        // Arrange
        var comment = "   Excellent hotel   ";

        // Act
        var review = new Review(
            bookingId: 1,
            rating: 5,
            comment);

        // Assert
        Assert.Equal(comment, review.Comment);
    }

    [Fact]
    public void Constructor_ShouldHaveDefaultReviewId()
    {
        // Act
        var review = CreateValidReview();

        // Assert
        Assert.Equal(0, review.ReviewId);
    }

    [Fact]
    public void Constructor_ShouldHaveNullBookingByDefault()
    {
        // Act
        var review = CreateValidReview();

        // Assert
        Assert.Null(review.Booking);
    }

    private static Review CreateValidReview()
    {
        return new Review(
            bookingId: 1,
            rating: 5,
            comment: "Excellent hotel");
    }
}