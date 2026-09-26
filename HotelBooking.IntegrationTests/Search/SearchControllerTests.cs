using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HotelBooking.Application.Common;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Search.Dtos;
using HotelBooking.Domain.Entities;
using HotelBooking.IntegrationTests.Infrastructure;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.IntegrationTests.Search;

public class SearchControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly DateTime CheckIn = new(2035, 6, 10);
    private static readonly DateTime CheckOut = new(2035, 6, 13);
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public SearchControllerTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Search_WithoutToken_ShouldReturnUnauthorized()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync(BuildUrl("Anywhere"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Search_WithAdminToken_ShouldReturnForbidden()
    {
        using var client = CreateAuthenticatedClient(await CreateUserAsync(Role.Admin));
        var response = await client.GetAsync(BuildUrl("Anywhere"));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(InvalidSearch.EmptyDestination)]
    [InlineData(InvalidSearch.WhitespaceDestination)]
    [InlineData(InvalidSearch.EqualDates)]
    [InlineData(InvalidSearch.CheckOutBeforeCheckIn)]
    [InlineData(InvalidSearch.ZeroAdults)]
    [InlineData(InvalidSearch.NegativeChildren)]
    [InlineData(InvalidSearch.ZeroRooms)]
    [InlineData(InvalidSearch.NegativeMinPrice)]
    [InlineData(InvalidSearch.NegativeMaxPrice)]
    [InlineData(InvalidSearch.MinPriceAboveMaxPrice)]
    [InlineData(InvalidSearch.MinRatingBelowRange)]
    [InlineData(InvalidSearch.MinRatingAboveRange)]
    [InlineData(InvalidSearch.NonPositiveAmenity)]
    [InlineData(InvalidSearch.ZeroPage)]
    public async Task Search_WithInvalidRequest_ShouldReturnBadRequest(InvalidSearch invalid)
    {
        using var client = CreateAuthenticatedClient(await CreateUserAsync(Role.Customer));
        var url = BuildInvalidUrl(invalid);
        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Search_WhenNothingMatches_ShouldReturnEmptyFirstPage()
    {
        using var client = CreateAuthenticatedClient(await CreateUserAsync(Role.Customer));
        var response = await client.GetAsync(BuildUrl(Unique("Missing")));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await ReadAsync(response);
        Assert.Empty(result.Items);
        Assert.Equal(1, result.PageNumber);
        Assert.False(result.HasNextPage);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Search_ShouldMatchDestinationByHotelNameOrCityName(bool byHotelName)
    {
        var token = Unique("Destination");
        var hotel = await CreateHotelAsync(byHotelName ? $"Hotel {token}" : Unique("Hotel"), byHotelName ? Unique("City") : $"City {token}");
        await CreateRoomAsync(hotel.HotelId);
        using var client = CreateAuthenticatedClient(await CreateUserAsync(Role.Customer));
        var response = await client.GetAsync(BuildUrl(token));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(hotel.HotelId, Assert.Single((await ReadAsync(response)).Items).HotelId);
    }

    [Fact]
    public async Task Search_ShouldTrimDestinationAndMatchCaseInsensitively()
    {
        var token = Unique("MixedCase");
        var hotel = await CreateHotelAsync(token.ToUpperInvariant());
        await CreateRoomAsync(hotel.HotelId);
        using var client = CreateAuthenticatedClient(await CreateUserAsync(Role.Customer));
        var response = await client.GetAsync(BuildUrl($"  {token.ToLowerInvariant()}  "));
        Assert.Equal(hotel.HotelId, Assert.Single((await ReadAsync(response)).Items).HotelId);
    }

    [Fact]
    public async Task Search_ShouldMapAllResponseFieldsAndLowestEligiblePrice()
    {
        var token = Unique("Mapped");
        var hotel = await CreateHotelAsync(token, "Mapped City", HotelType.Boutique, description: "Brief description");
        await CreateRoomAsync(hotel.HotelId, price: 190m);
        await CreateRoomAsync(hotel.HotelId, price: 120m);
        await CreateHotelImageAsync(hotel.HotelId, "https://test/second.jpg", 2);
        await CreateHotelImageAsync(hotel.HotelId, "https://test/first.jpg", 1);
        var reviewedRoom = await CreateRoomAsync(hotel.HotelId, price: 150m);
        await CreateReviewedBookingAsync(reviewedRoom, 4, BookingStatus.Completed);
        using var client = CreateAuthenticatedClient(await CreateUserAsync(Role.Customer));

        var response = await client.GetAsync(BuildUrl(token));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var mapped = Assert.Single((await ReadAsync(response)).Items);
        Assert.Equal(hotel.HotelId, mapped.HotelId);
        Assert.Equal(hotel.Name, mapped.Name);
        Assert.Equal("Mapped City", mapped.City);
        Assert.Equal(hotel.Address, mapped.Address);
        Assert.Equal(HotelType.Boutique, mapped.HotelType);
        Assert.Equal(120m, mapped.StartingPrice);
        Assert.Equal(4d, mapped.Rating);
        Assert.Equal("https://test/first.jpg", mapped.ThumbnailUrl);
        Assert.Equal("Brief description", mapped.BriefDescription);
    }

    [Fact]
    public async Task Search_WithoutReviewsOrImages_ShouldMapNullableFieldsToNull()
    {
        var token = Unique("Nullable");
        var hotel = await CreateHotelAsync(token);
        await CreateRoomAsync(hotel.HotelId);
        using var client = CreateAuthenticatedClient(await CreateUserAsync(Role.Customer));
        var mapped = Assert.Single((await ReadAsync(await client.GetAsync(BuildUrl(token)))).Items);
        Assert.Null(mapped.Rating);
        Assert.Null(mapped.ThumbnailUrl);
    }

    [Fact]
    public async Task Search_ShouldExcludeInactiveHotels()
    {
        var token = Unique("InactiveHotel");
        var hotel = await CreateHotelAsync(token, isActive: false);
        await CreateRoomAsync(hotel.HotelId);
        await AssertNoResultsAsync(BuildUrl(token));
    }

    [Theory]
    [InlineData(RoomDisqualification.Inactive)]
    [InlineData(RoomDisqualification.OperationallyUnavailable)]
    [InlineData(RoomDisqualification.InsufficientAdults)]
    [InlineData(RoomDisqualification.InsufficientChildren)]
    [InlineData(RoomDisqualification.BelowMinPrice)]
    [InlineData(RoomDisqualification.AboveMaxPrice)]
    [InlineData(RoomDisqualification.WrongType)]
    public async Task Search_ShouldExcludeHotelWhenItsOnlyRoomIsIneligible(RoomDisqualification reason)
    {
        var token = Unique("IneligibleRoom");
        var hotel = await CreateHotelAsync(token);
        var room = await CreateRoomAsync(hotel.HotelId,
            type: reason == RoomDisqualification.WrongType ? RoomType.Single : RoomType.Double,
            price: reason == RoomDisqualification.BelowMinPrice ? 50m : reason == RoomDisqualification.AboveMaxPrice ? 250m : 100m,
            adults: reason == RoomDisqualification.InsufficientAdults ? 1 : 2,
            children: reason == RoomDisqualification.InsufficientChildren ? 0 : 1);
        if (reason == RoomDisqualification.Inactive) room.ChangeStatus(false);
        if (reason == RoomDisqualification.OperationallyUnavailable) room.ChangeOperationalAvailability(false);
        await UpdateRoomAsync(room);
        var extra = reason switch
        {
            RoomDisqualification.BelowMinPrice => "&minPrice=75",
            RoomDisqualification.AboveMaxPrice => "&maxPrice=200",
            RoomDisqualification.WrongType => "&roomType=Double",
            _ => ""
        };
        await AssertNoResultsAsync(BuildUrl(token, adults: 2, children: 1) + extra);
    }

    [Fact]
    public async Task Search_ShouldRequireRequestedNumberOfEligibleRooms()
    {
        var token = Unique("RoomCount");
        var hotel = await CreateHotelAsync(token);
        await CreateRoomAsync(hotel.HotelId);
        await AssertNoResultsAsync(BuildUrl(token, rooms: 2));
        await CreateRoomAsync(hotel.HotelId);
        await AssertSingleResultAsync(BuildUrl(token, rooms: 2), hotel.HotelId);
    }

    [Theory]
    [InlineData(BookingStatus.Pending, true)]
    [InlineData(BookingStatus.Confirmed, true)]
    [InlineData(BookingStatus.Completed, true)]
    [InlineData(BookingStatus.Cancelled, false)]
    public async Task Search_ShouldRespectOverlappingBookingStatus(BookingStatus status, bool excludesRoom)
    {
        var token = Unique("Booked");
        var hotel = await CreateHotelAsync(token);
        var room = await CreateRoomAsync(hotel.HotelId);
        await CreateBookingAsync(room, CheckIn.AddDays(1), CheckOut.AddDays(1), status);
        if (excludesRoom) await AssertNoResultsAsync(BuildUrl(token));
        else await AssertSingleResultAsync(BuildUrl(token), hotel.HotelId);
    }

    [Theory]
    [InlineData(-3, -1)]
    [InlineData(3, 5)]
    [InlineData(-2, 0)]
    [InlineData(3, 6)]
    public async Task Search_ShouldAllowBookingsThatDoNotOverlap(int bookingCheckInOffset, int bookingCheckOutOffset)
    {
        var token = Unique("NonOverlap");
        var hotel = await CreateHotelAsync(token);
        var room = await CreateRoomAsync(hotel.HotelId);
        await CreateBookingAsync(room, CheckIn.AddDays(bookingCheckInOffset), CheckIn.AddDays(bookingCheckOutOffset), BookingStatus.Confirmed);
        await AssertSingleResultAsync(BuildUrl(token), hotel.HotelId);
    }

    [Fact]
    public async Task Search_ShouldFilterByHotelType()
    {
        var token = Unique("HotelType");
        var matching = await CreateHotelAsync(token, type: HotelType.Luxury);
        var other = await CreateHotelAsync(token, type: HotelType.Budget);
        await CreateRoomAsync(matching.HotelId);
        await CreateRoomAsync(other.HotelId);
        using var client = CreateAuthenticatedClient(await CreateUserAsync(Role.Customer));
        var items = (await ReadAsync(await client.GetAsync(BuildUrl(token) + "&hotelType=Luxury"))).Items.ToList();
        Assert.Equal(matching.HotelId, Assert.Single(items).HotelId);
    }

    [Fact]
    public async Task Search_WithAmenities_ShouldRequireAllRequestedAmenities()
    {
        var token = Unique("Amenities");
        var both = await CreateHotelAsync(token);
        var one = await CreateHotelAsync(token);
        await CreateRoomAsync(both.HotelId);
        await CreateRoomAsync(one.HotelId);
        var wifi = await CreateAmenityAsync("Wifi");
        var pool = await CreateAmenityAsync("Pool");
        await AddAmenityAsync(both.HotelId, wifi.AmenityId);
        await AddAmenityAsync(both.HotelId, pool.AmenityId);
        await AddAmenityAsync(one.HotelId, wifi.AmenityId);
        using var client = CreateAuthenticatedClient(await CreateUserAsync(Role.Customer));
        var url = BuildUrl(token) + $"&amenityIds={wifi.AmenityId}&amenityIds={pool.AmenityId}";
        var items = (await ReadAsync(await client.GetAsync(url))).Items.ToList();
        Assert.Equal(both.HotelId, Assert.Single(items).HotelId);
    }

    [Fact]
    public async Task Search_MinRating_ShouldUseOnlyCompletedReviewedBookings()
    {
        var token = Unique("Rating");
        var matching = await CreateHotelAsync(token);
        var excluded = await CreateHotelAsync(token);
        var matchingRoom = await CreateRoomAsync(matching.HotelId);
        var excludedRoom = await CreateRoomAsync(excluded.HotelId);
        await CreateReviewedBookingAsync(matchingRoom, 5, BookingStatus.Completed);
        await CreateReviewedBookingAsync(matchingRoom, 1, BookingStatus.Cancelled);
        await CreateReviewedBookingAsync(excludedRoom, 3, BookingStatus.Completed);
        using var client = CreateAuthenticatedClient(await CreateUserAsync(Role.Customer));
        var items = (await ReadAsync(await client.GetAsync(BuildUrl(token) + "&minRating=4"))).Items.ToList();
        Assert.Equal(matching.HotelId, Assert.Single(items).HotelId);
    }

    [Fact]
    public async Task Search_MinRating_ShouldExcludeHotelsWithoutCompletedReviews()
    {
        var token = Unique("NoRating");
        var hotel = await CreateHotelAsync(token);
        await CreateRoomAsync(hotel.HotelId);
        await AssertNoResultsAsync(BuildUrl(token) + "&minRating=1");
    }

    [Fact]
    public async Task Search_ShouldReturnTenItemsOrderedByHotelIdAndSetHasNextPage()
    {
        var token = Unique("Paging");
        var ids = new List<int>();
        for (var index = 0; index < 11; index++)
        {
            var hotel = await CreateHotelAsync(token);
            await CreateRoomAsync(hotel.HotelId);
            ids.Add(hotel.HotelId);
        }
        using var client = CreateAuthenticatedClient(await CreateUserAsync(Role.Customer));
        var first = await ReadAsync(await client.GetAsync(BuildUrl(token)));
        Assert.Equal(10, first.Items.Count());
        Assert.Equal(ids.Take(10), first.Items.Select(item => item.HotelId));
        Assert.True(first.HasNextPage);
        Assert.Equal(1, first.PageNumber);
        var second = await ReadAsync(await client.GetAsync(BuildUrl(token, page: 2)));
        Assert.Equal(ids[10], Assert.Single(second.Items).HotelId);
        Assert.False(second.HasNextPage);
        Assert.Equal(2, second.PageNumber);
    }

    private async Task AssertNoResultsAsync(string url)
    {
        using var client = CreateAuthenticatedClient(await CreateUserAsync(Role.Customer));
        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty((await ReadAsync(response)).Items);
    }

    private async Task AssertSingleResultAsync(string url, int hotelId)
    {
        using var client = CreateAuthenticatedClient(await CreateUserAsync(Role.Customer));
        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(hotelId, Assert.Single((await ReadAsync(response)).Items).HotelId);
    }

    private static string BuildUrl(string destination, int adults = 2, int children = 0, int rooms = 1, int page = 1) =>
        $"/api/Search?destination={Uri.EscapeDataString(destination)}&checkIn={CheckIn:O}&checkOut={CheckOut:O}&adults={adults}&children={children}&rooms={rooms}&pageNumber={page}";

    private static string BuildInvalidUrl(InvalidSearch invalid)
    {
        var destination = invalid == InvalidSearch.EmptyDestination ? "" : invalid == InvalidSearch.WhitespaceDestination ? "   " : "Valid";
        var checkIn = CheckIn;
        var checkOut = invalid == InvalidSearch.EqualDates ? CheckIn : invalid == InvalidSearch.CheckOutBeforeCheckIn ? CheckIn.AddDays(-1) : CheckOut;
        var adults = invalid == InvalidSearch.ZeroAdults ? 0 : 2;
        var children = invalid == InvalidSearch.NegativeChildren ? -1 : 0;
        var rooms = invalid == InvalidSearch.ZeroRooms ? 0 : 1;
        var page = invalid == InvalidSearch.ZeroPage ? 0 : 1;
        var url = BuildUrl(destination, adults, children, rooms, page).Replace($"checkOut={CheckOut:O}", $"checkOut={checkOut:O}");
        return invalid switch
        {
            InvalidSearch.NegativeMinPrice => url + "&minPrice=-1",
            InvalidSearch.NegativeMaxPrice => url + "&maxPrice=-1",
            InvalidSearch.MinPriceAboveMaxPrice => url + "&minPrice=200&maxPrice=100",
            InvalidSearch.MinRatingBelowRange => url + "&minRating=0",
            InvalidSearch.MinRatingAboveRange => url + "&minRating=6",
            InvalidSearch.NonPositiveAmenity => url + "&amenityIds=0",
            _ => url
        };
    }

    private async Task<User> CreateUserAsync(Role role)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var user = new User("Test", "User", Unique("user"), $"{Guid.NewGuid():N}@test.com", hasher.HashPassword("Password123"), role);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private async Task<Hotel> CreateHotelAsync(string name, string? cityName = null, HotelType type = HotelType.Luxury, bool isActive = true, string? description = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var city = new City(cityName ?? Unique("City"), "Palestine", Unique("PO"));
        db.Cities.Add(city);
        await db.SaveChangesAsync();
        var hotel = new Hotel(name, "Owner", "Address", 32.2, 35.2, type, city.CityId, description, null);
        if (!isActive) hotel.ChangeStatus(false);
        db.Hotels.Add(hotel);
        await db.SaveChangesAsync();
        return hotel;
    }

    private async Task<Room> CreateRoomAsync(int hotelId, RoomType type = RoomType.Double, decimal price = 100m, int adults = 2, int children = 1)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var room = new Room(Unique("Room"), type, price, adults, children, hotelId, null);
        db.Rooms.Add(room);
        await db.SaveChangesAsync();
        return room;
    }

    private async Task UpdateRoomAsync(Room room)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        db.Rooms.Update(room);
        await db.SaveChangesAsync();
    }

    private async Task<HotelImage> CreateHotelImageAsync(int hotelId, string url, int order)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var image = new HotelImage(url, order, hotelId);
        db.HotelImages.Add(image);
        await db.SaveChangesAsync();
        return image;
    }

    private async Task<Amenity> CreateAmenityAsync(string name)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var amenity = new Amenity(Unique(name), null);
        db.Amenities.Add(amenity);
        await db.SaveChangesAsync();
        return amenity;
    }

    private async Task AddAmenityAsync(int hotelId, int amenityId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        db.HotelAmenities.Add(new HotelAmenity { HotelId = hotelId, AmenityId = amenityId });
        await db.SaveChangesAsync();
    }

    private async Task<Booking> CreateBookingAsync(Room room, DateTime checkIn, DateTime checkOut, BookingStatus status)
    {
        var user = await CreateUserAsync(Role.Customer);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var invoice = new Invoice(user.UserId, room.HotelId, 100m);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
        var booking = new Booking(user.UserId, room.RoomId, checkIn, checkOut, 1, 0, 100m, 100m, 0, 0m, 100m, null)
        {
            InvoiceId = invoice.InvoiceId,
            BookingStatus = status
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        return booking;
    }

    private async Task CreateReviewedBookingAsync(Room room, int rating, BookingStatus status)
    {
        var booking = await CreateBookingAsync(room, CheckIn.AddYears(-1), CheckOut.AddYears(-1), status);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        db.Reviews.Add(new Review(booking.BookingId, rating, "Review"));
        await db.SaveChangesAsync();
    }

    private HttpClient CreateAuthenticatedClient(User user)
    {
        using var scope = _factory.Services.CreateScope();
        var generator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", generator.GenerateToken(user));
        return client;
    }

    private static async Task<PagedResult<HotelSearchResponseDto>> ReadAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<PagedResult<HotelSearchResponseDto>>(JsonOptions) ?? new PagedResult<HotelSearchResponseDto>();

    private static string Unique(string prefix) => $"{prefix}_{Guid.NewGuid():N}";

    public enum InvalidSearch { EmptyDestination, WhitespaceDestination, EqualDates, CheckOutBeforeCheckIn, ZeroAdults, NegativeChildren, ZeroRooms, NegativeMinPrice, NegativeMaxPrice, MinPriceAboveMaxPrice, MinRatingBelowRange, MinRatingAboveRange, NonPositiveAmenity, ZeroPage }
    public enum RoomDisqualification { Inactive, OperationallyUnavailable, InsufficientAdults, InsufficientChildren, BelowMinPrice, AboveMaxPrice, WrongType }
}
