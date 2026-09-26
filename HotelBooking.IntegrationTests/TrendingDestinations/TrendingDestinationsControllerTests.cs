using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.TrendingDestinations.Dtos;
using HotelBooking.Domain.Entities;
using HotelBooking.IntegrationTests.Infrastructure;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.IntegrationTests.TrendingDestinations;

public class TrendingDestinationsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string Endpoint = "/api/hotels/trending-destinations";
    private readonly CustomWebApplicationFactory _factory;

    public TrendingDestinationsControllerTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetTrendingDestinations_WithoutToken_ShouldReturnUnauthorized()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync(Endpoint);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetTrendingDestinations_WithAdminToken_ShouldReturnForbidden()
    {
        using var client = CreateAuthenticatedClient(await CreateUserAsync(Role.Admin));
        var response = await client.GetAsync(Endpoint);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetTrendingDestinations_WhenThereAreNoBookings_ShouldReturnEmptyArray()
    {
        await ClearBookingsAsync();
        using var client = CreateAuthenticatedClient(await CreateUserAsync(Role.Customer));
        var response = await client.GetAsync(Endpoint);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(await ReadAsync(response));
    }

    [Fact]
    public async Task GetTrendingDestinations_ShouldMapCityAndBookingCount()
    {
        await ClearBookingsAsync();
        var city = await CreateCityAsync("Bethlehem", "Palestine");
        var hotel = await CreateHotelAsync(city.CityId);
        var room = await CreateRoomAsync(hotel.HotelId);
        await CreateBookingAsync(room, DateTime.UtcNow.AddDays(-2));
        await CreateBookingAsync(room, DateTime.UtcNow.AddDays(-1));
        using var client = CreateAuthenticatedClient(await CreateUserAsync(Role.Customer));

        var response = await client.GetAsync(Endpoint);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var destination = Assert.Single(await ReadAsync(response));
        Assert.Equal(city.CityId, destination.CityId);
        Assert.Equal("Bethlehem", destination.Name);
        Assert.Equal("Palestine", destination.Country);
        Assert.Equal(2, destination.BookingCount);
    }

    [Fact]
    public async Task GetTrendingDestinations_ShouldAggregateBookingsAcrossHotelsAndRoomsInSameCity()
    {
        await ClearBookingsAsync();
        var city = await CreateCityAsync();
        var firstHotel = await CreateHotelAsync(city.CityId);
        var secondHotel = await CreateHotelAsync(city.CityId);
        var firstRoom = await CreateRoomAsync(firstHotel.HotelId);
        var secondRoom = await CreateRoomAsync(secondHotel.HotelId);
        await CreateBookingAsync(firstRoom, DateTime.UtcNow.AddDays(-1));
        await CreateBookingAsync(firstRoom, DateTime.UtcNow.AddDays(-2));
        await CreateBookingAsync(secondRoom, DateTime.UtcNow.AddDays(-3));
        using var client = CreateAuthenticatedClient(await CreateUserAsync(Role.Customer));

        var destination = Assert.Single(await ReadAsync(await client.GetAsync(Endpoint)));

        Assert.Equal(city.CityId, destination.CityId);
        Assert.Equal(3, destination.BookingCount);
    }

    [Fact]
    public async Task GetTrendingDestinations_ShouldIncludeOnlyBookingsCreatedWithinLastThirtyDays()
    {
        await ClearBookingsAsync();
        var recentCity = await CreateCityAsync("Recent");
        var oldCity = await CreateCityAsync("Old");
        var recentRoom = await CreateRoomAsync((await CreateHotelAsync(recentCity.CityId)).HotelId);
        var oldRoom = await CreateRoomAsync((await CreateHotelAsync(oldCity.CityId)).HotelId);
        await CreateBookingAsync(recentRoom, DateTime.UtcNow.AddDays(-29));
        await CreateBookingAsync(oldRoom, DateTime.UtcNow.AddDays(-31));
        using var client = CreateAuthenticatedClient(await CreateUserAsync(Role.Customer));

        var destinations = await ReadAsync(await client.GetAsync(Endpoint));

        Assert.Equal(recentCity.CityId, Assert.Single(destinations).CityId);
        Assert.DoesNotContain(destinations, item => item.CityId == oldCity.CityId);
    }

    [Theory]
    [InlineData(BookingStatus.Pending)]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Completed)]
    [InlineData(BookingStatus.Cancelled)]
    public async Task GetTrendingDestinations_ShouldCountEveryBookingStatus(BookingStatus status)
    {
        await ClearBookingsAsync();
        var city = await CreateCityAsync();
        var room = await CreateRoomAsync((await CreateHotelAsync(city.CityId)).HotelId);
        await CreateBookingAsync(room, DateTime.UtcNow.AddDays(-1), status);
        using var client = CreateAuthenticatedClient(await CreateUserAsync(Role.Customer));

        var destination = Assert.Single(await ReadAsync(await client.GetAsync(Endpoint)));

        Assert.Equal(city.CityId, destination.CityId);
        Assert.Equal(1, destination.BookingCount);
    }

    [Fact]
    public async Task GetTrendingDestinations_ShouldCountBookingsForInactiveHotelsAndRooms()
    {
        await ClearBookingsAsync();
        var city = await CreateCityAsync();
        var hotel = await CreateHotelAsync(city.CityId, false);
        var room = await CreateRoomAsync(hotel.HotelId, false, false);
        await CreateBookingAsync(room, DateTime.UtcNow.AddDays(-1));
        using var client = CreateAuthenticatedClient(await CreateUserAsync(Role.Customer));

        var destination = Assert.Single(await ReadAsync(await client.GetAsync(Endpoint)));

        Assert.Equal(city.CityId, destination.CityId);
        Assert.Equal(1, destination.BookingCount);
    }

    [Fact]
    public async Task GetTrendingDestinations_ShouldOrderByBookingCountDescendingAndTakeFive()
    {
        await ClearBookingsAsync();
        var expected = new List<(int CityId, int Count)>();
        for (var count = 1; count <= 6; count++)
        {
            var city = await CreateCityAsync($"City {count}");
            var room = await CreateRoomAsync((await CreateHotelAsync(city.CityId)).HotelId);
            for (var booking = 0; booking < count; booking++)
                await CreateBookingAsync(room, DateTime.UtcNow.AddDays(-1));
            expected.Add((city.CityId, count));
        }
        using var client = CreateAuthenticatedClient(await CreateUserAsync(Role.Customer));

        var destinations = await ReadAsync(await client.GetAsync(Endpoint));

        Assert.Equal(5, destinations.Count);
        Assert.Equal(expected.OrderByDescending(item => item.Count).Take(5).Select(item => item.CityId), destinations.Select(item => item.CityId));
        Assert.Equal(new[] { 6, 5, 4, 3, 2 }, destinations.Select(item => item.BookingCount));
        Assert.DoesNotContain(destinations, item => item.CityId == expected.Single(entry => entry.Count == 1).CityId);
    }

    private async Task ClearBookingsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        db.Reviews.RemoveRange(db.Reviews);
        db.Bookings.RemoveRange(db.Bookings);
        await db.SaveChangesAsync();
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

    private async Task<City> CreateCityAsync(string? name = null, string country = "Palestine")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var city = new City(name ?? Unique("City"), country, Unique("PO"));
        db.Cities.Add(city);
        await db.SaveChangesAsync();
        return city;
    }

    private async Task<Hotel> CreateHotelAsync(int cityId, bool isActive = true)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var hotel = new Hotel(Unique("Hotel"), "Owner", "Address", 32.2, 35.2, HotelType.Luxury, cityId, null, null);
        if (!isActive) hotel.ChangeStatus(false);
        db.Hotels.Add(hotel);
        await db.SaveChangesAsync();
        return hotel;
    }

    private async Task<Room> CreateRoomAsync(int hotelId, bool isActive = true, bool operational = true)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var room = new Room(Unique("Room"), RoomType.Double, 100m, 2, 1, hotelId, null);
        if (!isActive) room.ChangeStatus(false);
        if (!operational) room.ChangeOperationalAvailability(false);
        db.Rooms.Add(room);
        await db.SaveChangesAsync();
        return room;
    }

    private async Task<Booking> CreateBookingAsync(Room room, DateTime createdAt, BookingStatus status = BookingStatus.Confirmed)
    {
        var user = await CreateUserAsync(Role.Customer);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var invoice = new Invoice(user.UserId, room.HotelId, 100m);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
        var booking = new Booking(user.UserId, room.RoomId, new DateTime(2035, 1, 1), new DateTime(2035, 1, 2), 1, 0, 100m, 100m, 0, 0m, 100m, null)
        {
            InvoiceId = invoice.InvoiceId,
            BookingStatus = status,
            CreatedAt = createdAt
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        return booking;
    }

    private HttpClient CreateAuthenticatedClient(User user)
    {
        using var scope = _factory.Services.CreateScope();
        var generator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", generator.GenerateToken(user));
        return client;
    }

    private static async Task<List<TrendingDestinationResponseDto>> ReadAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<List<TrendingDestinationResponseDto>>() ?? [];

    private static string Unique(string prefix) => $"{prefix}_{Guid.NewGuid():N}";
}
