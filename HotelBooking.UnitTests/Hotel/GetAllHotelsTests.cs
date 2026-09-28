using HotelBooking.Application.Common;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Hotels.Dtos;
using HotelBooking.Application.Hotels.Retrive;
using HotelBooking.Application.Interfaces;
using Moq;

namespace HotelBooking.UnitTests.Hotels.Retrieve;

public class GetAllHotelsTests
{
    private readonly Mock<IHotelRepository> _hotelRepositoryMock;
    private readonly GetAllHotels _service;

    public GetAllHotelsTests()
    {
        _hotelRepositoryMock = new Mock<IHotelRepository>();
        _service = new GetAllHotels(_hotelRepositoryMock.Object);
    }

    [Fact]
    public async Task GetAllHotelsAsync_WhenNoHotelsExist_ShouldReturnEmptyPage()
    {
        var request = new HotelListRequestDto();
        _hotelRepositoryMock
            .Setup(repository => repository.GetHotelsAsync(request))
            .ReturnsAsync(new PagedResult<HotelResponseDto> { Items = [], PageNumber = 1 });

        var result = await _service.GetAllHotelsAsync(request);

        Assert.Empty(result.Items);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public async Task GetAllHotelsAsync_WhenPageExists_ShouldReturnRepositoryResult()
    {
        var request = new HotelListRequestDto { Search = "Grand", PageNumber = 2 };
        var expected = new PagedResult<HotelResponseDto>
        {
            Items = [new HotelResponseDto { HotelId = 10, Name = "Grand Hotel", RoomsCount = 12 }],
            PageNumber = 2,
            HasNextPage = true
        };
        _hotelRepositoryMock
            .Setup(repository => repository.GetHotelsAsync(request))
            .ReturnsAsync(expected);

        var result = await _service.GetAllHotelsAsync(request);

        Assert.Same(expected, result);
        _hotelRepositoryMock.Verify(repository => repository.GetHotelsAsync(request), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetAllHotelsAsync_WhenPageNumberIsInvalid_ShouldThrowBadRequestException(int pageNumber)
    {
        var request = new HotelListRequestDto { PageNumber = pageNumber };

        var action = async () => await _service.GetAllHotelsAsync(request);

        await Assert.ThrowsAsync<BadRequestException>(action);
        _hotelRepositoryMock.Verify(
            repository => repository.GetHotelsAsync(It.IsAny<HotelListRequestDto>()),
            Times.Never);
    }
}
