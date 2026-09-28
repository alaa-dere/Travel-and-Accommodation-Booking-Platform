using HotelBooking.Application.Interfaces;
using HotelBooking.Application.RecentlyVisitedHotels;
using HotelBooking.Domain.Entities;
using Moq;

namespace HotelBooking.UnitTests.RecentlyVisitedHotels;

public class RecordHotelVisitServiceTests
{
    private static readonly DateTimeOffset UtcNow = new(2030, 1, 15, 10, 30, 0, TimeSpan.Zero);
    private readonly Mock<IRecentlyVisitedHotelRepository> _repositoryMock;
    private readonly RecordHotelVisitService _service;

    public RecordHotelVisitServiceTests()
    {
        _repositoryMock = new Mock<IRecentlyVisitedHotelRepository>();

        _service = new RecordHotelVisitService(
            _repositoryMock.Object,
            new FixedTimeProvider(UtcNow));
    }

    [Fact]
    public async Task RecordVisitAsync_WhenVisitExists_ShouldUpdateVisitedAt()
    {
        // Arrange
        const int userId = 10;
        const int hotelId = 20;

        var oldVisitedAt = UtcNow.UtcDateTime.AddDays(-5);

        var existingVisit = new RecentlyVisitedHotel
        {
            UserId = userId,
            HotelId = hotelId,
            VisitedAt = oldVisitedAt
        };

        _repositoryMock
            .Setup(repository =>
                repository.GetVisitAsync(userId, hotelId))
            .ReturnsAsync(existingVisit);

        // Act
        await _service.RecordVisitAsync(userId, hotelId);

        // Assert
        Assert.Equal(UtcNow.UtcDateTime, existingVisit.VisitedAt);
    }

    [Fact]
    public async Task RecordVisitAsync_WhenVisitExists_ShouldNotAddNewVisit()
    {
        // Arrange
        const int userId = 10;
        const int hotelId = 20;

        var existingVisit = new RecentlyVisitedHotel
        {
            UserId = userId,
            HotelId = hotelId,
            VisitedAt = UtcNow.UtcDateTime.AddDays(-1)
        };

        _repositoryMock
            .Setup(repository =>
                repository.GetVisitAsync(userId, hotelId))
            .ReturnsAsync(existingVisit);

        // Act
        await _service.RecordVisitAsync(userId, hotelId);

        // Assert
        _repositoryMock.Verify(
            repository => repository.AddAsync(
                It.IsAny<RecentlyVisitedHotel>()),
            Times.Never);
    }

    [Fact]
    public async Task RecordVisitAsync_WhenVisitDoesNotExist_ShouldCreateNewVisitWithCorrectData()
    {
        // Arrange
        const int userId = 10;
        const int hotelId = 20;

        _repositoryMock
            .Setup(repository =>
                repository.GetVisitAsync(userId, hotelId))
            .ReturnsAsync((RecentlyVisitedHotel?)null);

        RecentlyVisitedHotel? addedVisit = null;

        _repositoryMock
            .Setup(repository =>
                repository.AddAsync(
                    It.IsAny<RecentlyVisitedHotel>()))
            .Callback<RecentlyVisitedHotel>(visit =>
                addedVisit = visit);

        // Act
        await _service.RecordVisitAsync(userId, hotelId);

        // Assert
        Assert.NotNull(addedVisit);

        Assert.Equal(userId, addedVisit!.UserId);
        Assert.Equal(hotelId, addedVisit.HotelId);

        Assert.Equal(UtcNow.UtcDateTime, addedVisit.VisitedAt);
    }

    [Fact]
    public async Task RecordVisitAsync_WhenVisitDoesNotExist_ShouldAddVisitOnce()
    {
        // Arrange
        const int userId = 10;
        const int hotelId = 20;

        _repositoryMock
            .Setup(repository =>
                repository.GetVisitAsync(userId, hotelId))
            .ReturnsAsync((RecentlyVisitedHotel?)null);

        // Act
        await _service.RecordVisitAsync(userId, hotelId);

        // Assert
        _repositoryMock.Verify(
            repository => repository.AddAsync(
                It.Is<RecentlyVisitedHotel>(visit =>
                    visit.UserId == userId &&
                    visit.HotelId == hotelId)),
            Times.Once);
    }

    [Fact]
    public async Task RecordVisitAsync_WhenVisitExists_ShouldSaveChangesOnce()
    {
        // Arrange
        const int userId = 10;
        const int hotelId = 20;

        var existingVisit = new RecentlyVisitedHotel
        {
            UserId = userId,
            HotelId = hotelId,
            VisitedAt = UtcNow.UtcDateTime.AddDays(-1)
        };

        _repositoryMock
            .Setup(repository =>
                repository.GetVisitAsync(userId, hotelId))
            .ReturnsAsync(existingVisit);

        // Act
        await _service.RecordVisitAsync(userId, hotelId);

        // Assert
        _repositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task RecordVisitAsync_WhenVisitDoesNotExist_ShouldSaveChangesOnce()
    {
        // Arrange
        const int userId = 10;
        const int hotelId = 20;

        _repositoryMock
            .Setup(repository =>
                repository.GetVisitAsync(userId, hotelId))
            .ReturnsAsync((RecentlyVisitedHotel?)null);

        // Act
        await _service.RecordVisitAsync(userId, hotelId);

        // Assert
        _repositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task RecordVisitAsync_ShouldRequestVisitForCorrectUserAndHotel()
    {
        // Arrange
        const int userId = 10;
        const int hotelId = 20;

        _repositoryMock
            .Setup(repository =>
                repository.GetVisitAsync(userId, hotelId))
            .ReturnsAsync((RecentlyVisitedHotel?)null);

        // Act
        await _service.RecordVisitAsync(userId, hotelId);

        // Assert
        _repositoryMock.Verify(
            repository =>
                repository.GetVisitAsync(userId, hotelId),
            Times.Once);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
