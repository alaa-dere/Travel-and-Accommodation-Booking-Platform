using HotelBooking.Domain.Entities;

namespace HotelBooking.UnitTests.Domain.Entities;

public class PromotionTests
{
    [Fact]
    public void Constructor_WhenDataIsValid_ShouldCreatePromotionWithCorrectValues()
    {
        // Arrange
        var startDate = new DateTime(2026, 10, 1);
        var endDate = new DateTime(2026, 10, 31);

        var beforeCreation = DateTime.UtcNow;

        // Act
        var promotion = new Promotion(
            hotelId: 1,
            discountPercentage: 20,
            startDate: startDate,
            endDate: endDate);

        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(1, promotion.HotelId);
        Assert.Equal(20, promotion.DiscountPercentage);
        Assert.Equal(startDate, promotion.StartDate);
        Assert.Equal(endDate, promotion.EndDate);
        Assert.True(promotion.IsActive);

        Assert.InRange(
            promotion.CreatedAt,
            beforeCreation,
            afterCreation);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenHotelIdIsInvalid_ShouldThrowArgumentException(
        int hotelId)
    {
        // Act
        var action = () =>
            CreateValidPromotion(hotelId: hotelId);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(101)]
    public void Constructor_WhenDiscountPercentageIsInvalid_ShouldThrowArgumentOutOfRangeException(
        int discountPercentage)
    {
        // Act
        var action = () =>
            CreateValidPromotion(
                discountPercentage: discountPercentage);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(99)]
    public void Constructor_WhenDiscountPercentageIsAtValidBoundary_ShouldSucceed(
        int discountPercentage)
    {
        // Act
        var promotion = CreateValidPromotion(
            discountPercentage: discountPercentage);

        // Assert
        Assert.Equal(
            discountPercentage,
            promotion.DiscountPercentage);
    }

    [Fact]
    public void Constructor_WhenStartDateEqualsEndDate_ShouldThrowArgumentException()
    {
        // Arrange
        var date = new DateTime(2026, 10, 10);

        // Act
        var action = () =>
            new Promotion(
                hotelId: 1,
                discountPercentage: 20,
                startDate: date,
                endDate: date);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Constructor_WhenStartDateIsAfterEndDate_ShouldThrowArgumentException()
    {
        // Act
        var action = () =>
            new Promotion(
                hotelId: 1,
                discountPercentage: 20,
                startDate: new DateTime(2026, 10, 20),
                endDate: new DateTime(2026, 10, 10));

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Constructor_WhenEndDateIsAfterStartDate_ShouldSucceed()
    {
        // Arrange
        var startDate = new DateTime(2026, 10, 10);
        var endDate = startDate.AddDays(1);

        // Act
        var promotion = new Promotion(
            hotelId: 1,
            discountPercentage: 20,
            startDate: startDate,
            endDate: endDate);

        // Assert
        Assert.Equal(startDate, promotion.StartDate);
        Assert.Equal(endDate, promotion.EndDate);
    }

    [Fact]
    public void Constructor_ShouldSetPromotionAsActive()
    {
        // Act
        var promotion = CreateValidPromotion();

        // Assert
        Assert.True(promotion.IsActive);
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var promotion = CreateValidPromotion();

        // Act
        promotion.Deactivate();

        // Assert
        Assert.False(promotion.IsActive);
    }

    [Fact]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var promotion = CreateValidPromotion();
        promotion.Deactivate();

        // Act
        promotion.Activate();

        // Assert
        Assert.True(promotion.IsActive);
    }

    private static Promotion CreateValidPromotion(
        int hotelId = 1,
        int discountPercentage = 20,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var validStartDate =
            startDate ?? new DateTime(2026, 10, 1);

        var validEndDate =
            endDate ?? new DateTime(2026, 10, 31);

        return new Promotion(
            hotelId,
            discountPercentage,
            validStartDate,
            validEndDate);
    }
}