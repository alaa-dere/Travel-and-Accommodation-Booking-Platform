using HotelBooking.Application;
using HotelBooking.Application.Common;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Search;
using HotelBooking.Application.Search.Dtos;
using HotelBooking.Domain.Entities;
using Moq;

namespace HotelBooking.UnitTests.Search;

public class SearchHotelsTests
{
    private readonly Mock<IHotelSearchRepository> _repositoryMock;
    private readonly SearchHotels _service;

    public SearchHotelsTests()
    {
        _repositoryMock = new Mock<IHotelSearchRepository>();
        _service = new SearchHotels(_repositoryMock.Object);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchHotelsAsync_WhenDestinationIsInvalid_ShouldThrowBadRequestException(
        string destination)
    {
        // Arrange
        var request = CreateValidRequest();
        request.Destination = destination;

        // Act
        var action = async () =>
            await _service.SearchHotelsAsync(request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        VerifyRepositoryWasNotCalled();
    }

    [Fact]
    public async Task SearchHotelsAsync_WhenCheckOutIsBeforeCheckIn_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();

        request.CheckIn = new DateTime(2026, 10, 10);
        request.CheckOut = new DateTime(2026, 10, 9);

        // Act
        var action = async () =>
            await _service.SearchHotelsAsync(request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        VerifyRepositoryWasNotCalled();
    }

    [Fact]
    public async Task SearchHotelsAsync_WhenCheckOutEqualsCheckIn_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();

        var date = new DateTime(2026, 10, 10);

        request.CheckIn = date;
        request.CheckOut = date;

        // Act
        var action = async () =>
            await _service.SearchHotelsAsync(request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        VerifyRepositoryWasNotCalled();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task SearchHotelsAsync_WhenAdultsIsInvalid_ShouldThrowBadRequestException(
        int adults)
    {
        // Arrange
        var request = CreateValidRequest();
        request.Adults = adults;

        // Act
        var action = async () =>
            await _service.SearchHotelsAsync(request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        VerifyRepositoryWasNotCalled();
    }

    [Fact]
    public async Task SearchHotelsAsync_WhenChildrenIsNegative_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Children = -1;

        // Act
        var action = async () =>
            await _service.SearchHotelsAsync(request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        VerifyRepositoryWasNotCalled();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task SearchHotelsAsync_WhenRoomsIsInvalid_ShouldThrowBadRequestException(
        int rooms)
    {
        // Arrange
        var request = CreateValidRequest();
        request.Rooms = rooms;

        // Act
        var action = async () =>
            await _service.SearchHotelsAsync(request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        VerifyRepositoryWasNotCalled();
    }

    [Fact]
    public async Task SearchHotelsAsync_WhenMinPriceIsNegative_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.MinPrice = -1m;

        // Act
        var action = async () =>
            await _service.SearchHotelsAsync(request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        VerifyRepositoryWasNotCalled();
    }

    [Fact]
    public async Task SearchHotelsAsync_WhenMaxPriceIsNegative_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.MaxPrice = -1m;

        // Act
        var action = async () =>
            await _service.SearchHotelsAsync(request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        VerifyRepositoryWasNotCalled();
    }

    [Fact]
    public async Task SearchHotelsAsync_WhenMinPriceIsGreaterThanMaxPrice_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();

        request.MinPrice = 300m;
        request.MaxPrice = 100m;

        // Act
        var action = async () =>
            await _service.SearchHotelsAsync(request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        VerifyRepositoryWasNotCalled();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task SearchHotelsAsync_WhenMinRatingIsInvalid_ShouldThrowBadRequestException(
        int rating)
    {
        // Arrange
        var request = CreateValidRequest();
        request.MinRating = rating;

        // Act
        var action = async () =>
            await _service.SearchHotelsAsync(request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        VerifyRepositoryWasNotCalled();
    }

    [Fact]
    public async Task SearchHotelsAsync_WhenAmenityIdIsZero_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.AmenityIds = new List<int> { 1, 0, 3 };

        // Act
        var action = async () =>
            await _service.SearchHotelsAsync(request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        VerifyRepositoryWasNotCalled();
    }

    [Fact]
    public async Task SearchHotelsAsync_WhenAmenityIdIsNegative_ShouldThrowBadRequestException()
    {
        // Arrange
        var request = CreateValidRequest();
        request.AmenityIds = new List<int> { 1, -2, 3 };

        // Act
        var action = async () =>
            await _service.SearchHotelsAsync(request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        VerifyRepositoryWasNotCalled();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task SearchHotelsAsync_WhenPageNumberIsInvalid_ShouldThrowBadRequestException(
        int pageNumber)
    {
        // Arrange
        var request = CreateValidRequest();
        request.PageNumber = pageNumber;

        // Act
        var action = async () =>
            await _service.SearchHotelsAsync(request);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        VerifyRepositoryWasNotCalled();
    }

    [Fact]
    public async Task SearchHotelsAsync_WhenNoHotelsFound_ShouldReturnEmptyItems()
    {
        // Arrange
        var request = CreateValidRequest();

        var repositoryResult = new PagedResult<HotelSearchResult>
        {
            Items = new List<HotelSearchResult>(),
            PageNumber = 1,
            HasNextPage = false
        };

        _repositoryMock
            .Setup(repository =>
                repository.GetCandidateHotelsAsync(request))
            .ReturnsAsync(repositoryResult);

        // Act
        var result = await _service.SearchHotelsAsync(request);

        // Assert
        Assert.Empty(result.Items);
        Assert.Equal(1, result.PageNumber);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public async Task SearchHotelsAsync_ShouldPassSameRequestToRepository()
    {
        // Arrange
        var request = CreateValidRequest();

        SetupRepository(
            request,
            new List<HotelSearchResult>());

        // Act
        await _service.SearchHotelsAsync(request);

        // Assert
        _repositoryMock.Verify(
            repository =>
                repository.GetCandidateHotelsAsync(request),
            Times.Once);
    }

    [Fact]
    public async Task SearchHotelsAsync_ShouldMapHotelCorrectly()
    {
        // Arrange
        var request = CreateValidRequest();

        var hotel = CreateHotel(
            hotelId: 10,
            name: "Test Hotel");

        AddRoom(
            hotel,
            roomId: 1,
            roomNumber: "101",
            price: 150m);

        var searchResult = new HotelSearchResult
        {
            Hotel = hotel,
            Rating = 4.5,
            ThumbnailUrl = "https://example.com/hotel.jpg"
        };

        SetupRepository(
            request,
            new List<HotelSearchResult> { searchResult });

        // Act
        var result = await _service.SearchHotelsAsync(request);

        var response = result.Items.Single();

        // Assert
        Assert.Equal(hotel.HotelId, response.HotelId);
        Assert.Equal(hotel.Name, response.Name);
        Assert.Equal(hotel.City.Name, response.City);
        Assert.Equal(hotel.Address, response.Address);
        Assert.Equal(hotel.HotelType, response.HotelType);
        Assert.Equal(hotel.Description, response.BriefDescription);
        Assert.Equal(4.5, response.Rating);
        Assert.Equal(
            "https://example.com/hotel.jpg",
            response.ThumbnailUrl);
    }

    [Fact]
    public async Task SearchHotelsAsync_ShouldUseLowestRoomPriceAsStartingPrice()
    {
        // Arrange
        var request = CreateValidRequest();

        var hotel = CreateHotel(
            hotelId: 10,
            name: "Test Hotel");

        AddRoom(hotel, 1, "101", 300m);
        AddRoom(hotel, 2, "102", 120m);
        AddRoom(hotel, 3, "103", 200m);

        var searchResult = new HotelSearchResult
        {
            Hotel = hotel
        };

        SetupRepository(
            request,
            new List<HotelSearchResult> { searchResult });

        // Act
        var result = await _service.SearchHotelsAsync(request);

        // Assert
        var response = result.Items.Single();

        Assert.Equal(120m, response.StartingPrice);
    }

    [Fact]
    public async Task SearchHotelsAsync_WhenRatingIsNull_ShouldMapNullRating()
    {
        // Arrange
        var request = CreateValidRequest();

        var hotel = CreateHotel(10, "Test Hotel");
        AddRoom(hotel, 1, "101", 100m);

        var searchResult = new HotelSearchResult
        {
            Hotel = hotel,
            Rating = null
        };

        SetupRepository(
            request,
            new List<HotelSearchResult> { searchResult });

        // Act
        var result = await _service.SearchHotelsAsync(request);

        // Assert
        Assert.Null(result.Items.Single().Rating);
    }

    [Fact]
    public async Task SearchHotelsAsync_WhenThumbnailIsNull_ShouldMapNullThumbnail()
    {
        // Arrange
        var request = CreateValidRequest();

        var hotel = CreateHotel(10, "Test Hotel");
        AddRoom(hotel, 1, "101", 100m);

        var searchResult = new HotelSearchResult
        {
            Hotel = hotel,
            ThumbnailUrl = null
        };

        SetupRepository(
            request,
            new List<HotelSearchResult> { searchResult });

        // Act
        var result = await _service.SearchHotelsAsync(request);

        // Assert
        Assert.Null(result.Items.Single().ThumbnailUrl);
    }

    [Fact]
    public async Task SearchHotelsAsync_ShouldReturnAllCandidateHotels()
    {
        // Arrange
        var request = CreateValidRequest();

        var firstHotel = CreateHotel(1, "Hotel One");
        AddRoom(firstHotel, 1, "101", 100m);

        var secondHotel = CreateHotel(2, "Hotel Two");
        AddRoom(secondHotel, 2, "201", 200m);

        var candidates = new List<HotelSearchResult>
        {
            new()
            {
                Hotel = firstHotel,
                Rating = 4.0
            },
            new()
            {
                Hotel = secondHotel,
                Rating = 4.5
            }
        };

        SetupRepository(request, candidates);

        // Act
        var result = await _service.SearchHotelsAsync(request);

        // Assert
        var items = result.Items.ToList();

        Assert.Equal(2, items.Count);
        Assert.Contains(items, hotel => hotel.HotelId == 1);
        Assert.Contains(items, hotel => hotel.HotelId == 2);
    }

    [Fact]
    public async Task SearchHotelsAsync_ShouldPreservePaginationInformation()
    {
        // Arrange
        var request = CreateValidRequest();
        request.PageNumber = 3;

        var repositoryResult = new PagedResult<HotelSearchResult>
        {
            Items = new List<HotelSearchResult>(),
            PageNumber = 3,
            HasNextPage = true
        };

        _repositoryMock
            .Setup(repository =>
                repository.GetCandidateHotelsAsync(request))
            .ReturnsAsync(repositoryResult);

        // Act
        var result = await _service.SearchHotelsAsync(request);

        // Assert
        Assert.Equal(3, result.PageNumber);
        Assert.True(result.HasNextPage);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public async Task SearchHotelsAsync_WhenMinRatingIsAtValidBoundary_ShouldSucceed(
        int rating)
    {
        // Arrange
        var request = CreateValidRequest();
        request.MinRating = rating;

        SetupRepository(
            request,
            new List<HotelSearchResult>());

        // Act
        var result = await _service.SearchHotelsAsync(request);

        // Assert
        Assert.NotNull(result);

        _repositoryMock.Verify(
            repository =>
                repository.GetCandidateHotelsAsync(request),
            Times.Once);
    }

    [Fact]
    public async Task SearchHotelsAsync_WhenMinAndMaxPriceAreEqual_ShouldSucceed()
    {
        // Arrange
        var request = CreateValidRequest();

        request.MinPrice = 150m;
        request.MaxPrice = 150m;

        SetupRepository(
            request,
            new List<HotelSearchResult>());

        // Act
        var result = await _service.SearchHotelsAsync(request);

        // Assert
        Assert.NotNull(result);

        _repositoryMock.Verify(
            repository =>
                repository.GetCandidateHotelsAsync(request),
            Times.Once);
    }

    private void SetupRepository(
        HotelSearchRequestDto request,
        List<HotelSearchResult> items)
    {
        _repositoryMock
            .Setup(repository =>
                repository.GetCandidateHotelsAsync(request))
            .ReturnsAsync(
                new PagedResult<HotelSearchResult>
                {
                    Items = items,
                    PageNumber = request.PageNumber,
                    HasNextPage = false
                });
    }

    private void VerifyRepositoryWasNotCalled()
    {
        _repositoryMock.Verify(
            repository =>
                repository.GetCandidateHotelsAsync(
                    It.IsAny<HotelSearchRequestDto>()),
            Times.Never);
    }

    private static HotelSearchRequestDto CreateValidRequest()
    {
        return new HotelSearchRequestDto
        {
            Destination = "Nablus",
            CheckIn = new DateTime(2026, 10, 10),
            CheckOut = new DateTime(2026, 10, 15),
            Adults = 2,
            Children = 1,
            Rooms = 1,
            MinPrice = 50m,
            MaxPrice = 500m,
            MinRating = 3,
            AmenityIds = new List<int> { 1, 2 },
            PageNumber = 1
        };
    }

    private static Hotel CreateHotel(
        int hotelId,
        string name)
    {
        var hotel = new Hotel(
            name: name,
            ownerName: "Test Owner",
            address: "Test Address",
            latitude: 32.2211,
            longitude: 35.2544,
            hotelType: (HotelType)1,
            cityId: 1,
            description: "Test description",
            history: null)
        {
            HotelId = hotelId
        };

        hotel.City = new City(
            "Nablus",
            "Palestine",
            null);

        return hotel;
    }

    private static void AddRoom(
        Hotel hotel,
        int roomId,
        string roomNumber,
        decimal price)
    {
        var room = new Room(
            roomNumber: roomNumber,
            roomType: (RoomType)1,
            pricePerNight: price,
            adultsCapacity: 2,
            childCapacity: 1,
            hotelId: hotel.HotelId,
            description: null)
        {
            RoomId = roomId,
            Hotel = hotel
        };

        hotel.Rooms.Add(room);
    }
}