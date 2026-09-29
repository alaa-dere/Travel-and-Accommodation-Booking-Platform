using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Hotels.Delete;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using Moq;

namespace HotelBooking.UnitTests.Hotels.ChangeStatus;

public class ChangeHotelStatusTests
{
    private readonly Mock<IHotelRepository> _hotelRepositoryMock;
    private readonly ChangeHotelStatus _service;

    public ChangeHotelStatusTests()
    {
        _hotelRepositoryMock = new Mock<IHotelRepository>();

        _service = new ChangeHotelStatus(
            _hotelRepositoryMock.Object);
    }

    [Fact]
    public async Task ChangeHotelStatusAsync_WhenHotelDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        const int hotelId = 10;

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(hotelId))
            .ReturnsAsync((Hotel?)null);

        // Act
        var action = async () =>
            await _service.ChangeHotelStatusAsync(
                hotelId,
                true);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);

        _hotelRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task ChangeHotelStatusAsync_WhenActivatingHotel_ShouldSetIsActiveToTrue()
    {
        // Arrange
        const int hotelId = 10;

        var hotel = CreateHotel(hotelId);
        hotel.ChangeStatus(false);

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(hotelId))
            .ReturnsAsync(hotel);

        // Act
        await _service.ChangeHotelStatusAsync(
            hotelId,
            true);

        // Assert
        Assert.True(hotel.IsActive);
    }

    [Fact]
    public async Task ChangeHotelStatusAsync_WhenDeactivatingHotel_ShouldSetIsActiveToFalse()
    {
        // Arrange
        const int hotelId = 10;

        var hotel = CreateHotel(hotelId);
        hotel.ChangeStatus(true);

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(hotelId))
            .ReturnsAsync(hotel);

        // Act
        await _service.ChangeHotelStatusAsync(
            hotelId,
            false);

        // Assert
        Assert.False(hotel.IsActive);
    }

    [Fact]
    public async Task ChangeHotelStatusAsync_WhenHotelExists_ShouldSaveChangesOnce()
    {
        // Arrange
        const int hotelId = 10;

        var hotel = CreateHotel(hotelId);

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(hotelId))
            .ReturnsAsync(hotel);

        // Act
        await _service.ChangeHotelStatusAsync(
            hotelId,
            false);

        // Assert
        _hotelRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task ChangeHotelStatusAsync_ShouldRequestCorrectHotel()
    {
        // Arrange
        const int hotelId = 10;

        var hotel = CreateHotel(hotelId);

        _hotelRepositoryMock
            .Setup(repository =>
                repository.GetHotelByIdAsync(hotelId))
            .ReturnsAsync(hotel);

        // Act
        await _service.ChangeHotelStatusAsync(
            hotelId,
            false);

        // Assert
        _hotelRepositoryMock.Verify(
            repository => repository.GetHotelByIdAsync(hotelId),
            Times.Once);
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
            description: "Test Description",
            history: "Test History")
        {
            HotelId = hotelId
        };
    }
}