using HotelBooking.Application.Cities.Delete;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using Moq;

namespace HotelBooking.UnitTests.Cities.Delete;

public class DeleteCityTests
{
    private readonly Mock<ICityRepository> _cityRepositoryMock;
    private readonly DeleteCity _service;

    public DeleteCityTests()
    {
        _cityRepositoryMock = new Mock<ICityRepository>();
        _service = new DeleteCity(_cityRepositoryMock.Object);
    }

    [Fact]
    public async Task DeleteCityAsync_WhenCityDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        const int cityId = 10;

        _cityRepositoryMock.Setup(repository => repository.GetCityByIdAsync(cityId)).ReturnsAsync((City?)null);

        // Act
        var action = async () => await _service.DeleteCityAsync(cityId);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);

        _cityRepositoryMock.Verify(repository => repository.HasHotelsAsync(It.IsAny<int>()), Times.Never);
        _cityRepositoryMock.Verify(repository => repository.DeleteCity(It.IsAny<City>()), Times.Never);
        _cityRepositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteCityAsync_WhenCityHasAssociatedHotels_ShouldThrowConflictException()
    {
        // Arrange
        const int cityId = 10;

        var city = CreateCity(cityId);

        _cityRepositoryMock.Setup(repository => repository.GetCityByIdAsync(cityId)).ReturnsAsync(city);
        _cityRepositoryMock.Setup(repository => repository.HasHotelsAsync(cityId)).ReturnsAsync(true);

        // Act
        var action = async () => await _service.DeleteCityAsync(cityId);

        // Assert
        await Assert.ThrowsAsync<ConflictException>(action);

        _cityRepositoryMock.Verify(repository => repository.HasHotelsAsync(cityId), Times.Once);
        _cityRepositoryMock.Verify(repository => repository.DeleteCity(It.IsAny<City>()), Times.Never);
        _cityRepositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteCityAsync_WhenCityHasNoAssociatedHotels_ShouldDeleteCity()
    {
        // Arrange
        const int cityId = 10;

        var city = CreateCity(cityId);

        _cityRepositoryMock.Setup(repository => repository.GetCityByIdAsync(cityId)).ReturnsAsync(city);
        _cityRepositoryMock.Setup(repository => repository.HasHotelsAsync(cityId)).ReturnsAsync(false);

        // Act
        await _service.DeleteCityAsync(cityId);

        // Assert
        _cityRepositoryMock.Verify(repository => repository.DeleteCity(city), Times.Once);
    }

    [Fact]
    public async Task DeleteCityAsync_WhenCityHasNoAssociatedHotels_ShouldSaveChanges()
    {
        // Arrange
        const int cityId = 10;

        var city = CreateCity(cityId);

        _cityRepositoryMock.Setup(repository => repository.GetCityByIdAsync(cityId)).ReturnsAsync(city);
        _cityRepositoryMock.Setup(repository => repository.HasHotelsAsync(cityId)).ReturnsAsync(false);

        // Act
        await _service.DeleteCityAsync(cityId);

        // Assert
        _cityRepositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteCityAsync_ShouldCheckHotelsForCorrectCity()
    {
        // Arrange
        const int cityId = 10;

        var city = CreateCity(cityId);

        _cityRepositoryMock.Setup(repository => repository.GetCityByIdAsync(cityId)).ReturnsAsync(city);
        _cityRepositoryMock.Setup(repository => repository.HasHotelsAsync(cityId)).ReturnsAsync(false);

        // Act
        await _service.DeleteCityAsync(cityId);

        // Assert
        _cityRepositoryMock.Verify(repository => repository.GetCityByIdAsync(cityId), Times.Once);
        _cityRepositoryMock.Verify(repository => repository.HasHotelsAsync(cityId), Times.Once);
    }

    private static City CreateCity(int cityId)
    {
        return new City(name: "Nablus", country: "Palestine", postOffice: "P400")
        {
            CityId = cityId
        };
    }
}