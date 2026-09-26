using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Promotions.Status;
using HotelBooking.Domain.Entities;
using Moq;

namespace HotelBooking.UnitTests.Promotions.Status;

public class ChangePromotionStatusServiceTests
{
    private readonly Mock<IPromotionRepository> _promotionRepositoryMock;
    private readonly ChangePromotionStatusService _service;

    public ChangePromotionStatusServiceTests()
    {
        _promotionRepositoryMock = new Mock<IPromotionRepository>();

        _service = new ChangePromotionStatusService(
            _promotionRepositoryMock.Object);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ChangeStatusAsync_WhenPromotionIdIsInvalid_ShouldThrowBadRequestException(
        int promotionId)
    {
        // Act
        var action = async () =>
            await _service.ChangeStatusAsync(
                promotionId,
                true);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        _promotionRepositoryMock.Verify(
            repository => repository.GetByIdAsync(It.IsAny<int>()),
            Times.Never);

        _promotionRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task ChangeStatusAsync_WhenPromotionDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        const int promotionId = 10;

        _promotionRepositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(promotionId))
            .ReturnsAsync((Promotion?)null);

        // Act
        var action = async () =>
            await _service.ChangeStatusAsync(
                promotionId,
                true);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);

        _promotionRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task ChangeStatusAsync_WhenActivatingPromotion_ShouldSetIsActiveToTrue()
    {
        // Arrange
        const int promotionId = 10;

        var promotion = CreatePromotion(promotionId);
        promotion.Deactivate();

        _promotionRepositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(promotionId))
            .ReturnsAsync(promotion);

        // Act
        await _service.ChangeStatusAsync(
            promotionId,
            true);

        // Assert
        Assert.True(promotion.IsActive);
    }

    [Fact]
    public async Task ChangeStatusAsync_WhenDeactivatingPromotion_ShouldSetIsActiveToFalse()
    {
        // Arrange
        const int promotionId = 10;

        var promotion = CreatePromotion(promotionId);
        promotion.Activate();

        _promotionRepositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(promotionId))
            .ReturnsAsync(promotion);

        // Act
        await _service.ChangeStatusAsync(
            promotionId,
            false);

        // Assert
        Assert.False(promotion.IsActive);
    }

    [Fact]
    public async Task ChangeStatusAsync_WhenPromotionExists_ShouldSaveChangesOnce()
    {
        // Arrange
        const int promotionId = 10;

        var promotion = CreatePromotion(promotionId);

        _promotionRepositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(promotionId))
            .ReturnsAsync(promotion);

        // Act
        await _service.ChangeStatusAsync(
            promotionId,
            false);

        // Assert
        _promotionRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task ChangeStatusAsync_ShouldRequestCorrectPromotion()
    {
        // Arrange
        const int promotionId = 10;

        var promotion = CreatePromotion(promotionId);

        _promotionRepositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(promotionId))
            .ReturnsAsync(promotion);

        // Act
        await _service.ChangeStatusAsync(
            promotionId,
            true);

        // Assert
        _promotionRepositoryMock.Verify(
            repository => repository.GetByIdAsync(promotionId),
            Times.Once);
    }

    private static Promotion CreatePromotion(int promotionId)
    {
        return new Promotion(
            hotelId: 10,
            discountPercentage: 20,
            startDate: new DateTime(2026, 10, 1),
            endDate: new DateTime(2026, 10, 31))
        {
            PromotionId = promotionId
        };
    }
}