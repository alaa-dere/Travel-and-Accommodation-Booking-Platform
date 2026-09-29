using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Promotions.Create;
using HotelBooking.Domain.Entities;
using Moq;

namespace HotelBooking.UnitTests.Promotions.Create;

public class CreatePromotionServiceTests
{
    private readonly Mock<IPromotionRepository> _promotionRepositoryMock;
    private readonly Mock<IHotelRepository> _hotelRepositoryMock;
    private readonly CreatePromotionService _service;

    public CreatePromotionServiceTests()
    {
        _promotionRepositoryMock = new Mock<IPromotionRepository>();
        _hotelRepositoryMock = new Mock<IHotelRepository>();

        _service = new CreatePromotionService(
            _promotionRepositoryMock.Object,
            _hotelRepositoryMock.Object);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateAsync_WhenHotelIdIsInvalid_ShouldThrowBadRequestException(
        int hotelId)
    {
        // Arrange
        var request = CreateValidRequest();
        request.HotelId = hotelId;

        // Act
        var action = async () =>
            await _service.CreateAsync(request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        _hotelRepositoryMock.Verify(
            repository => repository.GetHotelByIdAsync(It.IsAny<int>()),
            Times.Never);

        VerifyPromotionWasNotSaved();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(101)]
    public async Task CreateAsync_WhenDiscountPercentageIsInvalid_ShouldThrowBadRequestException(
        int discountPercentage)
    {
        // Arrange
        var request = CreateValidRequest();
        request.DiscountPercentage = discountPercentage;

        // Act
        var action = async () =>
            await _service.CreateAsync(request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        _hotelRepositoryMock.Verify(
            repository => repository.GetHotelByIdAsync(It.IsAny<int>()),
            Times.Never);

        VerifyPromotionWasNotSaved();
    }

    [Fact]
    public async Task CreateAsync_WhenStartDateIsAfterEndDate_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();

        request.StartDate = new DateTime(2026, 10, 20);
        request.EndDate = new DateTime(2026, 10, 10);

        // Act
        var action = async () =>
            await _service.CreateAsync(request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        _hotelRepositoryMock.Verify(
            repository => repository.GetHotelByIdAsync(It.IsAny<int>()),
            Times.Never);

        VerifyPromotionWasNotSaved();
    }

    [Fact]
    public async Task CreateAsync_WhenStartDateEqualsEndDate_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();

        var date = new DateTime(2026, 10, 10);

        request.StartDate = date;
        request.EndDate = date;

        // Act
        var action = async () =>
            await _service.CreateAsync(request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        _hotelRepositoryMock.Verify(
            repository => repository.GetHotelByIdAsync(It.IsAny<int>()),
            Times.Never);

        VerifyPromotionWasNotSaved();
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

        VerifyPromotionWasNotSaved();
    }

    [Fact]
    public async Task CreateAsync_WhenHotelDoesNotExist_ShouldNotAddOrSavePromotion()
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
        VerifyPromotionWasNotSaved();
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
    public async Task CreateAsync_WhenRequestIsValid_ShouldCreatePromotionWithCorrectData()
    {
        // Arrange
        var request = CreateValidRequest();
        var hotel = CreateHotel(request.HotelId);

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(request.HotelId))
            .ReturnsAsync(hotel);

        Promotion? addedPromotion = null;

        _promotionRepositoryMock
            .Setup(repository =>
                repository.AddAsync(It.IsAny<Promotion>()))
            .Callback<Promotion>(promotion =>
                addedPromotion = promotion);

        // Act
        await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(addedPromotion);

        Assert.Equal(
            request.HotelId,
            addedPromotion!.HotelId);

        Assert.Equal(
            request.DiscountPercentage,
            addedPromotion.DiscountPercentage);

        Assert.Equal(
            request.StartDate,
            addedPromotion.StartDate);

        Assert.Equal(
            request.EndDate,
            addedPromotion.EndDate);
    }

    [Fact]
    public async Task CreateAsync_WhenRequestIsValid_ShouldAddPromotionOnce()
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
        _promotionRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.Is<Promotion>(promotion =>
                    promotion.HotelId == request.HotelId &&
                    promotion.DiscountPercentage == request.DiscountPercentage &&
                    promotion.StartDate == request.StartDate &&
                    promotion.EndDate == request.EndDate)),
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
        _promotionRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(99)]
    public async Task CreateAsync_WhenDiscountIsAtValidBoundary_ShouldCreatePromotion(
        int discountPercentage)
    {
        // Arrange
        var request = CreateValidRequest();
        request.DiscountPercentage = discountPercentage;

        var hotel = CreateHotel(request.HotelId);

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(request.HotelId))
            .ReturnsAsync(hotel);

        // Act
        await _service.CreateAsync(request);

        // Assert
        _promotionRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.Is<Promotion>(promotion =>
                    promotion.DiscountPercentage == discountPercentage)),
            Times.Once);

        _promotionRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once);
    }

    private void VerifyPromotionWasNotSaved()
    {
        _promotionRepositoryMock.Verify(
            repository =>
                repository.AddAsync(It.IsAny<Promotion>()),
            Times.Never);

        _promotionRepositoryMock.Verify(
            repository =>
                repository.SaveChangesAsync(),
            Times.Never);
    }

    private static CreatePromotionRequestDto CreateValidRequest()
    {
        return new CreatePromotionRequestDto
        {
            HotelId = 10,
            DiscountPercentage = 20,
            StartDate = new DateTime(2026, 10, 1),
            EndDate = new DateTime(2026, 10, 31)
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