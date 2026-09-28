using HotelBooking.Application.Common;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Rooms.Dtos;
using HotelBooking.Application.Rooms.Retrive;
using Moq;

namespace HotelBooking.UnitTests.Rooms;

public class GetAllRoomsTests
{
    private readonly Mock<IRoomRepository> _roomRepositoryMock;
    private readonly GetAllRooms _service;

    public GetAllRoomsTests()
    {
        _roomRepositoryMock = new Mock<IRoomRepository>();
        _service = new GetAllRooms(_roomRepositoryMock.Object);
    }

    [Fact]
    public async Task GetAllRoomsAsync_WhenNoRoomsExist_ShouldReturnEmptyPage()
    {
        var filter = new RoomFilterDto();
        _roomRepositoryMock
            .Setup(repository => repository.GetRoomsAsync(filter))
            .ReturnsAsync(new PagedResult<RoomResponseDto> { Items = [], PageNumber = 1 });

        var result = await _service.GetAllRoomsAsync(filter);

        Assert.Empty(result.Items);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public async Task GetAllRoomsAsync_WhenPageExists_ShouldReturnRepositoryResult()
    {
        var filter = new RoomFilterDto { HotelId = 5, PageNumber = 2 };
        var expected = new PagedResult<RoomResponseDto>
        {
            Items = [new RoomResponseDto { RoomId = 1, HotelId = 5, HotelName = "Test Hotel" }],
            PageNumber = 2,
            HasNextPage = true
        };
        _roomRepositoryMock
            .Setup(repository => repository.GetRoomsAsync(filter))
            .ReturnsAsync(expected);

        var result = await _service.GetAllRoomsAsync(filter);

        Assert.Same(expected, result);
        _roomRepositoryMock.Verify(repository => repository.GetRoomsAsync(filter), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetAllRoomsAsync_WhenPageNumberIsInvalid_ShouldThrowBadRequestException(int pageNumber)
    {
        var filter = new RoomFilterDto { PageNumber = pageNumber };

        var action = async () => await _service.GetAllRoomsAsync(filter);

        await Assert.ThrowsAsync<BadRequestException>(action);
        _roomRepositoryMock.Verify(
            repository => repository.GetRoomsAsync(It.IsAny<RoomFilterDto>()),
            Times.Never);
    }
}
