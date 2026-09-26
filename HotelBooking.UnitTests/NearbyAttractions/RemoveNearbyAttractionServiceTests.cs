using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.NearbyAttractions.Remove;
using HotelBooking.Domain.Entities;
using Moq;

namespace HotelBooking.UnitTests.NearbyAttractions.Remove;

public class RemoveNearbyAttractionServiceTests
{
    private readonly Mock<INearbyAttractionRepository> _attractionRepositoryMock;
    private readonly RemoveNearbyAttractionService _service;

    public RemoveNearbyAttractionServiceTests()
    {
        _attractionRepositoryMock = new Mock<INearbyAttractionRepository>();

        _service = new RemoveNearbyAttractionService(
            _attractionRepositoryMock.Object);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task RemoveAsync_WhenAttractionIdIsInvalid_ShouldThrowBadRequestException(
        int attractionId)
    {
        // Act
        var action = async () =>
            await _service.RemoveAsync(attractionId);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        _attractionRepositoryMock.Verify(
            repository => repository.GetByIdAsync(It.IsAny<int>()),
            Times.Never);

        _attractionRepositoryMock.Verify(
            repository => repository.Remove(It.IsAny<NearbyAttraction>()),
            Times.Never);

        _attractionRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task RemoveAsync_WhenAttractionDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        const int attractionId = 10;

        _attractionRepositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(attractionId))
            .ReturnsAsync((NearbyAttraction?)null);

        // Act
        var action = async () =>
            await _service.RemoveAsync(attractionId);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);

        _attractionRepositoryMock.Verify(
            repository => repository.Remove(
                It.IsAny<NearbyAttraction>()),
            Times.Never);

        _attractionRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task RemoveAsync_WhenAttractionExists_ShouldRemoveAttraction()
    {
        // Arrange
        const int attractionId = 10;

        var attraction = CreateAttraction(attractionId);

        _attractionRepositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(attractionId))
            .ReturnsAsync(attraction);

        // Act
        await _service.RemoveAsync(attractionId);

        // Assert
        _attractionRepositoryMock.Verify(
            repository => repository.Remove(attraction),
            Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_WhenAttractionExists_ShouldSaveChangesOnce()
    {
        // Arrange
        const int attractionId = 10;

        var attraction = CreateAttraction(attractionId);

        _attractionRepositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(attractionId))
            .ReturnsAsync(attraction);

        // Act
        await _service.RemoveAsync(attractionId);

        // Assert
        _attractionRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_ShouldRequestCorrectAttraction()
    {
        // Arrange
        const int attractionId = 10;

        var attraction = CreateAttraction(attractionId);

        _attractionRepositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(attractionId))
            .ReturnsAsync(attraction);

        // Act
        await _service.RemoveAsync(attractionId);

        // Assert
        _attractionRepositoryMock.Verify(
            repository => repository.GetByIdAsync(attractionId),
            Times.Once);
    }

    private static NearbyAttraction CreateAttraction(int attractionId)
    {
        return new NearbyAttraction(
            hotelId: 1,
            name: "Old City",
            description: "Historic area",
            latitude: 32.2211,
            longitude: 35.2544)
        {
            NearbyAttractionId = attractionId
        };
    }
}