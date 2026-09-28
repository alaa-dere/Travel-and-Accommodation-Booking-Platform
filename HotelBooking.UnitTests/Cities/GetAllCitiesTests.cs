using HotelBooking.Application.Cities;
using HotelBooking.Application.Common;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using Moq;

namespace HotelBooking.UnitTests.Cities;

public class GetAllCitiesTests
{
    private readonly Mock<ICityRepository> _cityRepositoryMock;
    private readonly GetAllCities _service;

    public GetAllCitiesTests()
    {
        _cityRepositoryMock = new Mock<ICityRepository>();
        _service = new GetAllCities(_cityRepositoryMock.Object);
    }

    [Fact]
    public async Task GetAllCitiesAsync_WhenNoCitiesExist_ShouldReturnEmptyPage()
    {
        var request = new CityListRequestDto();
        _cityRepositoryMock
            .Setup(repository => repository.GetCitiesAsync(request))
            .ReturnsAsync(new PagedResult<CityResponseDto>
            {
                Items = [],
                PageNumber = 1,
                HasNextPage = false
            });

        var result = await _service.GetAllCitiesAsync(request);

        Assert.Empty(result.Items);
        Assert.Equal(1, result.PageNumber);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public async Task GetAllCitiesAsync_WhenPageExists_ShouldReturnRepositoryResult()
    {
        var request = new CityListRequestDto { Search = "Nablus", PageNumber = 2 };
        var expected = new PagedResult<CityResponseDto>
        {
            Items = [new CityResponseDto { CityId = 1, Name = "Nablus", HotelsCount = 3 }],
            PageNumber = 2,
            HasNextPage = true
        };
        _cityRepositoryMock
            .Setup(repository => repository.GetCitiesAsync(request))
            .ReturnsAsync(expected);

        var result = await _service.GetAllCitiesAsync(request);

        Assert.Same(expected, result);
        _cityRepositoryMock.Verify(repository => repository.GetCitiesAsync(request), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetAllCitiesAsync_WhenPageNumberIsInvalid_ShouldThrowBadRequestException(int pageNumber)
    {
        var request = new CityListRequestDto { PageNumber = pageNumber };

        var action = async () => await _service.GetAllCitiesAsync(request);

        await Assert.ThrowsAsync<BadRequestException>(action);
        _cityRepositoryMock.Verify(
            repository => repository.GetCitiesAsync(It.IsAny<CityListRequestDto>()),
            Times.Never);
    }
}
