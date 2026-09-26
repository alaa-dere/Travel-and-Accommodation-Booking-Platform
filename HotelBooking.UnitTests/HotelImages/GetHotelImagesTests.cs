using HotelBooking.Application.Exceptions;
using HotelBooking.Application.HotelImages;
using HotelBooking.Application.HotelImages.Dtos;
using HotelBooking.Application.Interfaces;
using Moq;

namespace HotelBooking.UnitTests.HotelImages;

public class GetHotelImagesTests
{
    private readonly Mock<IHotelRepository> _hotelRepositoryMock;
    private readonly Mock<IHotelImageRepository> _hotelImageRepositoryMock;
    private readonly GetHotelImages _service;

    public GetHotelImagesTests()
    {
        _hotelRepositoryMock = new Mock<IHotelRepository>();
        _hotelImageRepositoryMock = new Mock<IHotelImageRepository>();

        _service = new GetHotelImages(_hotelRepositoryMock.Object, _hotelImageRepositoryMock.Object);
    }

    [Fact]
    public async Task GetHotelImagesAsync_WhenHotelIsNotActive_ShouldThrowNotFoundException()
    {
        // Arrange
        const int hotelId = 10;

        _hotelRepositoryMock
            .Setup(repository => repository.IsActiveHotelAsync(hotelId))
            .ReturnsAsync(false);

        // Act
        var action = async () =>
            await _service.GetHotelImagesAsync(hotelId);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);
    }

    [Fact]
    public async Task GetHotelImagesAsync_WhenHotelIsNotActive_ShouldNotRequestImages()
    {
        // Arrange
        const int hotelId = 10;

        _hotelRepositoryMock
            .Setup(repository => repository.IsActiveHotelAsync(hotelId))
            .ReturnsAsync(false);

        // Act
        try
        {
            await _service.GetHotelImagesAsync(hotelId);
        }
        catch (NotFoundException)
        {
        }

        // Assert
        _hotelImageRepositoryMock.Verify(repository => repository.GetHotelImagesAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetHotelImagesAsync_WhenHotelIsActive_ShouldReturnImages()
    {
        // Arrange
        const int hotelId = 10;

        var images = new List<HotelImageResponseDto>
        {
            new(),
            new()
        };

        _hotelRepositoryMock
            .Setup(repository => repository.IsActiveHotelAsync(hotelId))
            .ReturnsAsync(true);

        _hotelImageRepositoryMock
            .Setup(repository => repository.GetHotelImagesAsync(hotelId))
            .ReturnsAsync(images);

        // Act
        var result = await _service.GetHotelImagesAsync(hotelId);

        // Assert
        Assert.Same(images, result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetHotelImagesAsync_WhenHotelHasNoImages_ShouldReturnEmptyList()
    {
        // Arrange
        const int hotelId = 10;

        _hotelRepositoryMock
            .Setup(repository => repository.IsActiveHotelAsync(hotelId))
            .ReturnsAsync(true);

        _hotelImageRepositoryMock
            .Setup(repository => repository.GetHotelImagesAsync(hotelId))
            .ReturnsAsync(new List<HotelImageResponseDto>());

        // Act
        var result = await _service.GetHotelImagesAsync(hotelId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetHotelImagesAsync_ShouldCheckCorrectHotel()
    {
        // Arrange
        const int hotelId = 10;

        _hotelRepositoryMock
            .Setup(repository => repository.IsActiveHotelAsync(hotelId))
            .ReturnsAsync(true);

        _hotelImageRepositoryMock
            .Setup(repository => repository.GetHotelImagesAsync(hotelId))
            .ReturnsAsync(new List<HotelImageResponseDto>());

        // Act
        await _service.GetHotelImagesAsync(hotelId);

        // Assert
        _hotelRepositoryMock.Verify(repository => repository.IsActiveHotelAsync(hotelId), Times.Once);
    }

    [Fact]
    public async Task GetHotelImagesAsync_WhenHotelIsActive_ShouldRequestImagesForCorrectHotel()
    {
        // Arrange
        const int hotelId = 10;

        _hotelRepositoryMock
            .Setup(repository => repository.IsActiveHotelAsync(hotelId))
            .ReturnsAsync(true);

        _hotelImageRepositoryMock
            .Setup(repository => repository.GetHotelImagesAsync(hotelId))
            .ReturnsAsync(new List<HotelImageResponseDto>());

        // Act
        await _service.GetHotelImagesAsync(hotelId);

        // Assert
        _hotelImageRepositoryMock.Verify(repository => repository.GetHotelImagesAsync(hotelId), Times.Once);
    }
}