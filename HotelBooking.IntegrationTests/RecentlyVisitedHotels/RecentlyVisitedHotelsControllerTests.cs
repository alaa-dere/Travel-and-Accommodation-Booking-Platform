using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.RecentlyVisitedHotels.Dtos;
using HotelBooking.Domain.Entities;
using HotelBooking.IntegrationTests.Infrastructure;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.IntegrationTests.RecentlyVisitedHotels;

public class RecentlyVisitedHotelsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RecentlyVisitedHotelsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetRecentlyVisitedHotels_WithoutToken_ShouldReturnUnauthorized()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/hotels/recently-visited");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetRecentlyVisitedHotels_WithAdminToken_ShouldReturnForbidden()
    {
        var admin = await CreateUserAsync(Role.Admin);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.GetAsync("/api/hotels/recently-visited");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetRecentlyVisitedHotels_WhenCustomerHasNoVisits_ShouldReturnEmptyArray()
    {
        var customer = await CreateUserAsync(Role.Customer);
        using var client = CreateAuthenticatedClient(customer);
        var response = await client.GetAsync("/api/hotels/recently-visited");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<RecentlyVisitedHotelResponseDto>>();
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetRecentlyVisitedHotels_ShouldReturnOnlyAuthenticatedCustomersVisits()
    {
        var customer = await CreateUserAsync(Role.Customer);
        var otherCustomer = await CreateUserAsync(Role.Customer);
        var ownHotel = await CreateHotelAsync("Own City");
        var otherHotel = await CreateHotelAsync("Other City");
        await CreateVisitAsync(customer.UserId, ownHotel.HotelId, DateTime.UtcNow.AddDays(-1));
        await CreateVisitAsync(otherCustomer.UserId, otherHotel.HotelId, DateTime.UtcNow);
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync("/api/hotels/recently-visited");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<RecentlyVisitedHotelResponseDto>>();
        Assert.NotNull(result);
        var hotel = Assert.Single(result);
        Assert.Equal(ownHotel.HotelId, hotel.HotelId);
        Assert.Equal(ownHotel.Name, hotel.Name);
        Assert.Equal("Own City", hotel.City);
    }

    [Fact]
    public async Task GetRecentlyVisitedHotels_ShouldExcludeInactiveHotels()
    {
        var customer = await CreateUserAsync(Role.Customer);
        var activeHotel = await CreateHotelAsync();
        var inactiveHotel = await CreateHotelAsync(isActive: false);
        await CreateVisitAsync(customer.UserId, activeHotel.HotelId, DateTime.UtcNow.AddDays(-1));
        await CreateVisitAsync(customer.UserId, inactiveHotel.HotelId, DateTime.UtcNow);
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync("/api/hotels/recently-visited");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<RecentlyVisitedHotelResponseDto>>();
        Assert.NotNull(result);
        Assert.Equal(activeHotel.HotelId, Assert.Single(result).HotelId);
    }

    [Fact]
    public async Task GetRecentlyVisitedHotels_ShouldMapNullRatingAndNullPriceWhenNoEligibleDataExists()
    {
        var customer = await CreateUserAsync(Role.Customer);
        var hotel = await CreateHotelAsync();
        var inactiveRoom = await CreateRoomAsync(hotel.HotelId, 50m, false, true);
        var unavailableRoom = await CreateRoomAsync(hotel.HotelId, 60m, true, false);
        Assert.NotEqual(inactiveRoom.RoomId, unavailableRoom.RoomId);
        await CreateVisitAsync(customer.UserId, hotel.HotelId, DateTime.UtcNow);
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync("/api/hotels/recently-visited");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<RecentlyVisitedHotelResponseDto>>();
        Assert.NotNull(result);
        var mapped = Assert.Single(result);
        Assert.Null(mapped.Rating);
        Assert.Null(mapped.StartingPricePerNight);
    }

    [Fact]
    public async Task GetRecentlyVisitedHotels_ShouldUseMinimumActiveOperationalRoomPrice()
    {
        var customer = await CreateUserAsync(Role.Customer);
        var hotel = await CreateHotelAsync();
        await CreateRoomAsync(hotel.HotelId, 200m);
        await CreateRoomAsync(hotel.HotelId, 120m);
        await CreateRoomAsync(hotel.HotelId, 50m, false, true);
        await CreateRoomAsync(hotel.HotelId, 60m, true, false);
        await CreateVisitAsync(customer.UserId, hotel.HotelId, DateTime.UtcNow);
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync("/api/hotels/recently-visited");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<RecentlyVisitedHotelResponseDto>>();
        Assert.NotNull(result);
        Assert.Equal(120m, Assert.Single(result).StartingPricePerNight);
    }

    [Fact]
    public async Task GetRecentlyVisitedHotels_ShouldAverageOnlyCompletedReviewedBookings()
    {
        var customer = await CreateUserAsync(Role.Customer);
        var hotel = await CreateHotelAsync();
        var room = await CreateRoomAsync(hotel.HotelId, 100m);
        var firstCompleted = await CreateBookingAsync(customer, hotel, room, BookingStatus.Completed);
        var secondCompleted = await CreateBookingAsync(customer, hotel, room, BookingStatus.Completed);
        var pending = await CreateBookingAsync(customer, hotel, room, BookingStatus.Pending);
        await CreateReviewAsync(firstCompleted.BookingId, 5);
        await CreateReviewAsync(secondCompleted.BookingId, 2);
        await CreateReviewAsync(pending.BookingId, 1);
        await CreateVisitAsync(customer.UserId, hotel.HotelId, DateTime.UtcNow);
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync("/api/hotels/recently-visited");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<RecentlyVisitedHotelResponseDto>>();
        Assert.NotNull(result);
        Assert.Equal(3.5, Assert.Single(result).Rating);
    }

    [Fact]
    public async Task GetRecentlyVisitedHotels_ShouldOrderNewestFirstAndTakeFive()
    {
        var customer = await CreateUserAsync(Role.Customer);
        var expectedIds = new List<int>();
        for (var index = 0; index < 6; index++)
        {
            var hotel = await CreateHotelAsync();
            await CreateVisitAsync(customer.UserId, hotel.HotelId, DateTime.UtcNow.AddDays(-index));
            expectedIds.Add(hotel.HotelId);
        }
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync("/api/hotels/recently-visited");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<RecentlyVisitedHotelResponseDto>>();
        Assert.NotNull(result);
        Assert.Equal(5, result.Count);
        Assert.Equal(expectedIds.Take(5), result.Select(item => item.HotelId));
    }

    [Fact]
    public async Task GetRecentlyVisitedHotels_InactiveRecentVisitShouldNotConsumeFiveItemLimit()
    {
        var customer = await CreateUserAsync(Role.Customer);
        var inactive = await CreateHotelAsync(isActive: false);
        await CreateVisitAsync(customer.UserId, inactive.HotelId, DateTime.UtcNow.AddDays(1));
        var expectedIds = new List<int>();
        for (var index = 0; index < 5; index++)
        {
            var hotel = await CreateHotelAsync();
            await CreateVisitAsync(customer.UserId, hotel.HotelId, DateTime.UtcNow.AddDays(-index));
            expectedIds.Add(hotel.HotelId);
        }
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync("/api/hotels/recently-visited");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<RecentlyVisitedHotelResponseDto>>();
        Assert.NotNull(result);
        Assert.Equal(5, result.Count);
        Assert.Equal(expectedIds, result.Select(item => item.HotelId));
        Assert.DoesNotContain(result, item => item.HotelId == inactive.HotelId);
    }

    private async Task<User> CreateUserAsync(Role role)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var user = new User("Test", "User", Unique("user"), $"{Guid.NewGuid():N}@test.com",
            hasher.HashPassword("Password123"), role);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private async Task<Hotel> CreateHotelAsync(string? cityName = null, bool isActive = true)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var city = new City(cityName ?? Unique("City"), "Palestine", Unique("PO"));
        db.Cities.Add(city);
        await db.SaveChangesAsync();
        var hotel = new Hotel(Unique("Hotel"), "Owner", "Address", 32.2, 35.2,
            HotelType.Luxury, city.CityId, "Description", "History");
        if (!isActive) hotel.ChangeStatus(false);
        db.Hotels.Add(hotel);
        await db.SaveChangesAsync();
        return hotel;
    }

    private async Task<Room> CreateRoomAsync(
        int hotelId, decimal price, bool isActive = true, bool isOperational = true)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var room = new Room(Unique("Room"), RoomType.Double, price, 2, 1, hotelId, "Room");
        if (!isActive) room.ChangeStatus(false);
        if (!isOperational) room.ChangeOperationalAvailability(false);
        db.Rooms.Add(room);
        await db.SaveChangesAsync();
        return room;
    }

    private async Task<RecentlyVisitedHotel> CreateVisitAsync(
        int userId, int hotelId, DateTime visitedAt)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var visit = new RecentlyVisitedHotel { UserId = userId, HotelId = hotelId, VisitedAt = visitedAt };
        db.RecentlyVisitedHotels.Add(visit);
        await db.SaveChangesAsync();
        return visit;
    }

    private async Task<Booking> CreateBookingAsync(
        User user, Hotel hotel, Room room, BookingStatus status)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var invoice = new Invoice(user.UserId, hotel.HotelId, 200m);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
        var booking = new Booking(user.UserId, room.RoomId,
            new DateTime(2030, 1, 10), new DateTime(2030, 1, 12),
            2, 0, 100m, 200m, 0, 0m, 200m, null)
        {
            InvoiceId = invoice.InvoiceId,
            BookingStatus = status
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        return booking;
    }

    private async Task CreateReviewAsync(int bookingId, int rating)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        db.Reviews.Add(new Review(bookingId, rating, "Review"));
        await db.SaveChangesAsync();
    }

    private HttpClient CreateAuthenticatedClient(User user)
    {
        using var scope = _factory.Services.CreateScope();
        var generator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", generator.GenerateToken(user));
        return client;
    }

    private static string Unique(string prefix) => $"{prefix}_{Guid.NewGuid():N}";
}
