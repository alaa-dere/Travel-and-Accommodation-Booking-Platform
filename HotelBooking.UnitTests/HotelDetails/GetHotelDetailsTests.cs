using HotelBooking.Application.Exceptions;
using HotelBooking.Application.HotelDetails;
using HotelBooking.Application.HotelDetails.Dtos;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.RecentlyVisitedHotels;
using Moq;

namespace HotelBooking.UnitTests.HotelDetails;

public class GetHotelDetailsTests
{
    private readonly Mock<IHotelRepository> _hotelRepositoryMock;
    private readonly Mock<IRecordHotelVisitService> _recordHotelVisitServiceMock;
    private readonly GetHotelDetails _service;

    public GetHotelDetailsTests()
    {
        _hotelRepositoryMock = new Mock<IHotelRepository>();
        _recordHotelVisitServiceMock = new Mock<IRecordHotelVisitService>();
        _service = new GetHotelDetails(_hotelRepositoryMock.Object, _recordHotelVisitServiceMock.Object);
    }

    [Fact]
    public async Task GetHotelDetailsAsync_WhenHotelDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        const int hotelId = 10;
        const int userId = 5;

        _hotelRepositoryMock
            .Setup(repository => repository.GetHotelDetailsAsync(hotelId))
            .ReturnsAsync((HotelDetailsResponseDto?)null);

        // Act
        var action = async () => await _service.GetHotelDetailsAsync(hotelId, userId);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);

        _recordHotelVisitServiceMock.Verify(
            service => service.RecordVisitAsync(
                It.IsAny<int>(),
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetHotelDetailsAsync_WhenHotelExists_ShouldReturnHotelDetails()
    {
        // Arrange
        const int hotelId = 10;
        const int userId = 5;

        var hotelDetails = new HotelDetailsResponseDto();

        _hotelRepositoryMock
            .Setup(repository => repository.GetHotelDetailsAsync(hotelId))
            .ReturnsAsync(hotelDetails);

        // Act
        var result = await _service.GetHotelDetailsAsync(hotelId, userId);

        // Assert
        Assert.Same(hotelDetails, result);
    }

    [Fact]
    public async Task GetHotelDetailsAsync_WhenHotelExists_ShouldRecordVisit()
    {
        // Arrange
        const int hotelId = 10;
        const int userId = 5;

        var hotelDetails = new HotelDetailsResponseDto();

        _hotelRepositoryMock
            .Setup(repository => repository.GetHotelDetailsAsync(hotelId))
            .ReturnsAsync(hotelDetails);

        // Act
        await _service.GetHotelDetailsAsync(hotelId, userId);

        // Assert
        _recordHotelVisitServiceMock.Verify(
            service => service.RecordVisitAsync(userId, hotelId), Times.Once);
    }

    [Fact]
    public async Task GetHotelDetailsAsync_ShouldRequestCorrectHotelFromRepository()
    {
        // Arrange
        const int hotelId = 10;
        const int userId = 5;

        var hotelDetails = new HotelDetailsResponseDto();

        _hotelRepositoryMock
            .Setup(repository => repository.GetHotelDetailsAsync(hotelId))
            .ReturnsAsync(hotelDetails);

        // Act
        await _service.GetHotelDetailsAsync(hotelId, userId);

        // Assert
        _hotelRepositoryMock.Verify(repository => repository.GetHotelDetailsAsync(hotelId), Times.Once);
    }

    [Fact]
    public async Task GetHotelDetailsAsync_WhenHotelDoesNotExist_ShouldNotRecordVisit()
    {
        // Arrange
        const int hotelId = 10;
        const int userId = 5;

        _hotelRepositoryMock
            .Setup(repository => repository.GetHotelDetailsAsync(hotelId))
            .ReturnsAsync((HotelDetailsResponseDto?)null);

        // Act
        try
        {
            await _service.GetHotelDetailsAsync(hotelId, userId);
        }
        catch (NotFoundException)
        {
        }

        // Assert
        _recordHotelVisitServiceMock.Verify(service => service.RecordVisitAsync(userId, hotelId), Times.Never);
    }

    [Fact]
    public async Task GetHotelDetailsAsync_WhenRecordingVisitFails_ShouldPropagateException()
    {
        // Arrange
        const int hotelId = 10;
        const int userId = 5;

        var hotelDetails = new HotelDetailsResponseDto();

        _hotelRepositoryMock
            .Setup(repository => repository.GetHotelDetailsAsync(hotelId))
            .ReturnsAsync(hotelDetails);

        _recordHotelVisitServiceMock
            .Setup(service => service.RecordVisitAsync(userId, hotelId))
            .ThrowsAsync(new InvalidOperationException("Failed to record hotel visit."));

        // Act
        var action = async () => await _service.GetHotelDetailsAsync(hotelId, userId);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(action);
    }
}