using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.NearbyAttractions.Update;
using HotelBooking.Domain.Entities;
using Moq;

namespace HotelBooking.UnitTests.NearbyAttractions.Update;

public class UpdateNearbyAttractionServiceTests
{
    private readonly Mock<INearbyAttractionRepository> _attractionRepositoryMock;
    private readonly UpdateNearbyAttractionService _service;

    public UpdateNearbyAttractionServiceTests()
    {
        _attractionRepositoryMock = new Mock<INearbyAttractionRepository>();

        _service = new UpdateNearbyAttractionService(
            _attractionRepositoryMock.Object);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateAsync_WhenAttractionIdIsInvalid_ShouldThrowBadRequestException(
        int attractionId)
    {
        // Arrange
        var request = CreateValidRequest();

        // Act
        var action = async () =>
            await _service.UpdateAsync(attractionId, request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        _attractionRepositoryMock.Verify(
            repository => repository.GetByIdAsync(It.IsAny<int>()),
            Times.Never);

        _attractionRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenNameIsEmpty_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Name = string.Empty;

        // Act
        var action = async () =>
            await _service.UpdateAsync(1, request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        VerifyRepositoryWasNotCalled();
    }

    [Fact]
    public async Task UpdateAsync_WhenNameIsWhitespace_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Name = "   ";

        // Act
        var action = async () =>
            await _service.UpdateAsync(1, request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        VerifyRepositoryWasNotCalled();
    }

    [Fact]
    public async Task UpdateAsync_WhenNameExceeds100Characters_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Name = new string('A', 101);

        // Act
        var action = async () =>
            await _service.UpdateAsync(1, request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        VerifyRepositoryWasNotCalled();
    }

    [Fact]
    public async Task UpdateAsync_WhenDescriptionExceeds500Characters_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Description = new string('A', 501);

        // Act
        var action = async () =>
            await _service.UpdateAsync(1, request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        VerifyRepositoryWasNotCalled();
    }

    [Theory]
    [InlineData(-90.1)]
    [InlineData(90.1)]
    public async Task UpdateAsync_WhenLatitudeIsInvalid_ShouldThrowBadRequestException(
        double latitude)
    {
        // Arrange
        var request = CreateValidRequest();
        request.Latitude = latitude;

        // Act
        var action = async () =>
            await _service.UpdateAsync(1, request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        VerifyRepositoryWasNotCalled();
    }

    [Theory]
    [InlineData(-180.1)]
    [InlineData(180.1)]
    public async Task UpdateAsync_WhenLongitudeIsInvalid_ShouldThrowBadRequestException(
        double longitude)
    {
        // Arrange
        var request = CreateValidRequest();
        request.Longitude = longitude;

        // Act
        var action = async () =>
            await _service.UpdateAsync(1, request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        VerifyRepositoryWasNotCalled();
    }

    [Fact]
    public async Task UpdateAsync_WhenAttractionDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        const int attractionId = 10;
        var request = CreateValidRequest();

        _attractionRepositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(attractionId))
            .ReturnsAsync((NearbyAttraction?)null);

        // Act
        var action = async () =>
            await _service.UpdateAsync(attractionId, request);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);

        _attractionRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenRequestIsValid_ShouldUpdateAttractionFields()
    {
        // Arrange
        const int attractionId = 10;

        var attraction = CreateAttraction(attractionId);
        var request = CreateValidRequest();

        _attractionRepositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(attractionId))
            .ReturnsAsync(attraction);

        // Act
        await _service.UpdateAsync(attractionId, request);

        // Assert
        Assert.Equal(request.Name, attraction.Name);
        Assert.Equal(request.Description, attraction.Description);
        Assert.Equal(request.Latitude, attraction.Latitude);
        Assert.Equal(request.Longitude, attraction.Longitude);
    }

    [Fact]
    public async Task UpdateAsync_WhenRequestIsValid_ShouldSaveChangesOnce()
    {
        // Arrange
        const int attractionId = 10;

        var attraction = CreateAttraction(attractionId);
        var request = CreateValidRequest();

        _attractionRepositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(attractionId))
            .ReturnsAsync(attraction);

        // Act
        await _service.UpdateAsync(attractionId, request);

        // Assert
        _attractionRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldRequestCorrectAttraction()
    {
        // Arrange
        const int attractionId = 10;

        var attraction = CreateAttraction(attractionId);
        var request = CreateValidRequest();

        _attractionRepositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(attractionId))
            .ReturnsAsync(attraction);

        // Act
        await _service.UpdateAsync(attractionId, request);

        // Assert
        _attractionRepositoryMock.Verify(
            repository => repository.GetByIdAsync(attractionId),
            Times.Once);
    }

    [Theory]
    [InlineData(-90, -180)]
    [InlineData(90, 180)]
    public async Task UpdateAsync_WhenCoordinatesAreAtBoundaries_ShouldUpdateSuccessfully(
        double latitude,
        double longitude)
    {
        // Arrange
        const int attractionId = 10;

        var attraction = CreateAttraction(attractionId);

        var request = CreateValidRequest();
        request.Latitude = latitude;
        request.Longitude = longitude;

        _attractionRepositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(attractionId))
            .ReturnsAsync(attraction);

        // Act
        await _service.UpdateAsync(attractionId, request);

        // Assert
        Assert.Equal(latitude, attraction.Latitude);
        Assert.Equal(longitude, attraction.Longitude);

        _attractionRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenDescriptionIsNull_ShouldUpdateSuccessfully()
    {
        // Arrange
        const int attractionId = 10;

        var attraction = CreateAttraction(attractionId);

        var request = CreateValidRequest();
        request.Description = null;

        _attractionRepositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(attractionId))
            .ReturnsAsync(attraction);

        // Act
        await _service.UpdateAsync(attractionId, request);

        // Assert
        Assert.Null(attraction.Description);

        _attractionRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once);
    }

    private void VerifyRepositoryWasNotCalled()
    {
        _attractionRepositoryMock.Verify(
            repository => repository.GetByIdAsync(It.IsAny<int>()),
            Times.Never);

        _attractionRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    private static UpdateNearbyAttractionRequestDto CreateValidRequest()
    {
        return new UpdateNearbyAttractionRequestDto
        {
            Name = "Updated Attraction",
            Description = "Updated Description",
            Latitude = 32.2211,
            Longitude = 35.2544
        };
    }

    private static NearbyAttraction CreateAttraction(int attractionId)
    {
        return new NearbyAttraction(
            hotelId: 1,
            name: "Old Attraction",
            description: "Old Description",
            latitude: 31.0,
            longitude: 35.0)
        {
            NearbyAttractionId = attractionId
        };
    }
}