using HotelBooking.Application.AvailableRooms;
using HotelBooking.Application.AvailableRooms.Dtos;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using Moq;

namespace HotelBooking.UnitTests.AvailableRooms;

public class GetAvailableRoomsTests
{
    private static readonly DateTimeOffset UtcNow = new(2026, 9, 28, 12, 0, 0, TimeSpan.Zero);
    private readonly Mock<IHotelRepository> _hotelRepositoryMock;
    private readonly Mock<IAvailableRoomRepository> _availableRoomRepositoryMock;
    private readonly GetAvailableRooms _service;

    public GetAvailableRoomsTests()
    {
        _hotelRepositoryMock = new Mock<IHotelRepository>();
        _availableRoomRepositoryMock = new Mock<IAvailableRoomRepository>();
        _service = new GetAvailableRooms(
            _hotelRepositoryMock.Object,
            _availableRoomRepositoryMock.Object,
            new FixedTimeProvider(UtcNow));
    }

    [Fact]
    public async Task GetAvailableRoomsAsync_WhenCheckOutIsBeforeCheckIn_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.CheckOut = request.CheckIn.AddDays(-1);

        // Act
        var action = async () => await _service.GetAvailableRoomsAsync(1, request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);
        _hotelRepositoryMock.Verify(repository => repository.IsActiveHotelAsync(It.IsAny<int>()), Times.Never);

        _availableRoomRepositoryMock.Verify(
            repository => repository.GetAvailableRoomsAsync(
                It.IsAny<int>(),
                It.IsAny<RoomAvailabilityCriteria>()),
            Times.Never);
    }

    [Fact]
    public async Task GetAvailableRoomsAsync_WhenCheckOutEqualsCheckIn_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.CheckOut = request.CheckIn;

        // Act
        var action = async () => await _service.GetAvailableRoomsAsync(1, request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);
    }

    [Fact]
    public async Task GetAvailableRoomsAsync_WhenCheckInIsInThePast_ShouldThrowBadRequestException()
    {
        var request = CreateValidRequest();
        request.CheckIn = UtcNow.UtcDateTime.AddDays(-2);
        request.CheckOut = UtcNow.UtcDateTime.AddDays(-1);

        var action = async () => await _service.GetAvailableRoomsAsync(1, request);

        await Assert.ThrowsAsync<BadRequestException>(action);
        _hotelRepositoryMock.Verify(repository => repository.IsActiveHotelAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetAvailableRoomsAsync_WhenAdultsIsZero_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Adults = 0;

        // Act
        var action = async () => await _service.GetAvailableRoomsAsync(1, request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        _hotelRepositoryMock.Verify(repository => repository.IsActiveHotelAsync(It.IsAny<int>()), Times.Never);

        _availableRoomRepositoryMock.Verify(
            repository => repository.GetAvailableRoomsAsync(
                It.IsAny<int>(),
                It.IsAny<RoomAvailabilityCriteria>()),
            Times.Never);
    }

    [Fact]
    public async Task GetAvailableRoomsAsync_WhenAdultsIsNegative_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Adults = -1;

        // Act
        var action = async () => await _service.GetAvailableRoomsAsync(1, request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);
    }

    [Fact]
    public async Task GetAvailableRoomsAsync_WhenChildrenIsNegative_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Children = -1;

        // Act
        var action = async () =>
            await _service.GetAvailableRoomsAsync(1, request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        _hotelRepositoryMock.Verify(repository => repository.IsActiveHotelAsync(It.IsAny<int>()), Times.Never);

        _availableRoomRepositoryMock.Verify(
            repository => repository.GetAvailableRoomsAsync(
                It.IsAny<int>(),
                It.IsAny<RoomAvailabilityCriteria>()),
            Times.Never);
    }

    [Fact]
    public async Task GetAvailableRoomsAsync_WhenHotelIsNotActive_ShouldThrowNotFoundException()
    {
        // Arrange
        const int hotelId = 1;
        var request = CreateValidRequest();

        _hotelRepositoryMock.Setup(repository => repository.IsActiveHotelAsync(hotelId)).ReturnsAsync(false);

        // Act
        var action = async () => await _service.GetAvailableRoomsAsync(hotelId, request);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);

        _availableRoomRepositoryMock.Verify(
            repository => repository.GetAvailableRoomsAsync(
                It.IsAny<int>(),
                It.IsAny<RoomAvailabilityCriteria>()),
            Times.Never);
    }

    [Fact]
    public async Task GetAvailableRoomsAsync_WhenRequestIsValid_ShouldReturnAvailableRooms()
    {
        // Arrange
        const int hotelId = 1;
        var request = CreateValidRequest();
        var expectedRooms = new List<AvailableRoomResponseDto>();

        _hotelRepositoryMock.Setup(repository => repository.IsActiveHotelAsync(hotelId)).ReturnsAsync(true);
        _availableRoomRepositoryMock
            .Setup(repository =>
                repository.GetAvailableRoomsAsync(
                    hotelId,
                    It.Is<RoomAvailabilityCriteria>(criteria =>
                        criteria.CheckIn == request.CheckIn &&
                        criteria.CheckOut == request.CheckOut &&
                        criteria.Adults == request.Adults &&
                        criteria.Children == request.Children)))
            .ReturnsAsync(expectedRooms);

        // Act
        var result = await _service.GetAvailableRoomsAsync(hotelId, request);

        // Assert
        Assert.Same(expectedRooms, result);

        _hotelRepositoryMock.Verify(
            repository => repository.IsActiveHotelAsync(hotelId), Times.Once);

        _availableRoomRepositoryMock.Verify(
            repository => repository.GetAvailableRoomsAsync(
                hotelId,
                It.Is<RoomAvailabilityCriteria>(criteria =>
                    criteria.CheckIn == request.CheckIn &&
                    criteria.CheckOut == request.CheckOut &&
                    criteria.Adults == request.Adults &&
                    criteria.Children == request.Children)),
            Times.Once);
    }

    private static AvailableRoomsRequestDto CreateValidRequest()
    {
        return new AvailableRoomsRequestDto
        {
            CheckIn = UtcNow.UtcDateTime.AddDays(2),
            CheckOut = UtcNow.UtcDateTime.AddDays(4),
            Adults = 2,
            Children = 0
        };
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
