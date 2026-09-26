using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HotelBooking.Application.HotelDetails.Dtos;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.IntegrationTests.Infrastructure;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.IntegrationTests.HotelDetails;

public class HotelDetailsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public HotelDetailsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHotelDetails_WithoutToken_ShouldReturnUnauthorized()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/hotels/1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetHotelDetails_WithAdminToken_ShouldReturnForbidden()
    {
        var admin = await CreateUserAsync(Role.Admin);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.GetAsync("/api/hotels/1");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(999999)]
    public async Task GetHotelDetails_WhenHotelDoesNotExist_ShouldReturnNotFoundAndNotRecordVisit(int hotelId)
    {
        var customer = await CreateUserAsync(Role.Customer);
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync($"/api/hotels/{hotelId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, await CountVisitsAsync(customer.UserId, hotelId));
    }

    [Fact]
    public async Task GetHotelDetails_WhenHotelIsInactive_ShouldReturnNotFoundAndNotRecordVisit()
    {
        var customer = await CreateUserAsync(Role.Customer);
        var hotel = await CreateHotelAsync();
        await UpdateHotelAsync(hotel.HotelId, item => item.ChangeStatus(false));
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync($"/api/hotels/{hotel.HotelId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, await CountVisitsAsync(customer.UserId, hotel.HotelId));
    }

    [Fact]
    public async Task GetHotelDetails_WithValidHotel_ShouldMapAllFieldsAmenitiesAndCompletedReviewRating()
    {
        var customer = await CreateUserAsync(Role.Customer);
        var hotel = await CreateHotelAsync("Details City", "Hotel description", "Hotel history");
        await AddAmenitiesAsync(hotel.HotelId,
            ("Pool", "Indoor pool"),
            ("WiFi", null));
        var firstRoom = await CreateRoomAsync(hotel.HotelId);
        var secondRoom = await CreateRoomAsync(hotel.HotelId);
        var firstCompleted = await CreateBookingAsync(customer, hotel, firstRoom, BookingStatus.Completed);
        var secondCompleted = await CreateBookingAsync(customer, hotel, secondRoom, BookingStatus.Completed);
        var pending = await CreateBookingAsync(customer, hotel, firstRoom, BookingStatus.Pending);
        await CreateReviewAsync(firstCompleted.BookingId, 5);
        await CreateReviewAsync(secondCompleted.BookingId, 3);
        await CreateReviewAsync(pending.BookingId, 1);
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync($"/api/hotels/{hotel.HotelId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<HotelDetailsResponseDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(hotel.HotelId, result.HotelId);
        Assert.Equal(hotel.Name, result.Name);
        Assert.Equal("Details City", result.City);
        Assert.Equal("Hotel description", result.Description);
        Assert.Equal("Hotel history", result.History);
        Assert.Equal(hotel.HotelType, result.HotelType);
        Assert.Equal(hotel.Address, result.Address);
        Assert.Equal(hotel.Latitude, result.Latitude);
        Assert.Equal(hotel.Longitude, result.Longitude);
        Assert.Equal(4d, result.Rating);
        Assert.Equal(2, result.Amenities.Count);
        Assert.Contains(result.Amenities, item => item.Name == "Pool" && item.Description == "Indoor pool");
        Assert.Contains(result.Amenities, item => item.Name == "WiFi" && item.Description == null);
    }

    [Fact]
    public async Task GetHotelDetails_WithoutOptionalData_ShouldReturnNullsAndEmptyAmenities()
    {
        var customer = await CreateUserAsync(Role.Customer);
        var hotel = await CreateHotelAsync(description: null, history: null);
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync($"/api/hotels/{hotel.HotelId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<HotelDetailsResponseDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.Null(result.Description);
        Assert.Null(result.History);
        Assert.Null(result.Rating);
        Assert.Empty(result.Amenities);
    }

    [Fact]
    public async Task GetHotelDetails_WhenSuccessful_ShouldRecordVisitForCustomer()
    {
        var customer = await CreateUserAsync(Role.Customer);
        var hotel = await CreateHotelAsync();
        var beforeRequest = DateTime.UtcNow;
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync($"/api/hotels/{hotel.HotelId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var visit = await FindVisitAsync(customer.UserId, hotel.HotelId);
        Assert.NotNull(visit);
        Assert.True(visit.VisitedAt >= beforeRequest);
        Assert.True(visit.VisitedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task GetHotelDetails_WhenVisitedAgain_ShouldRefreshExistingVisitWithoutDuplicate()
    {
        var customer = await CreateUserAsync(Role.Customer);
        var hotel = await CreateHotelAsync();
        var oldVisit = await CreateVisitAsync(customer.UserId, hotel.HotelId, DateTime.UtcNow.AddDays(-5));
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync($"/api/hotels/{hotel.HotelId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, await CountVisitsAsync(customer.UserId, hotel.HotelId));
        var refreshed = await FindVisitAsync(customer.UserId, hotel.HotelId);
        Assert.NotNull(refreshed);
        Assert.Equal(oldVisit.RecentlyVisitedHotelId, refreshed.RecentlyVisitedHotelId);
        Assert.True(refreshed.VisitedAt > oldVisit.VisitedAt);
    }

    [Fact]
    public async Task GetHotelDetails_ShouldRecordVisitsIndependentlyForDifferentCustomers()
    {
        var firstCustomer = await CreateUserAsync(Role.Customer);
        var secondCustomer = await CreateUserAsync(Role.Customer);
        var hotel = await CreateHotelAsync();
        using var firstClient = CreateAuthenticatedClient(firstCustomer);
        using var secondClient = CreateAuthenticatedClient(secondCustomer);

        Assert.Equal(HttpStatusCode.OK,
            (await firstClient.GetAsync($"/api/hotels/{hotel.HotelId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await secondClient.GetAsync($"/api/hotels/{hotel.HotelId}")).StatusCode);

        Assert.Equal(1, await CountVisitsAsync(firstCustomer.UserId, hotel.HotelId));
        Assert.Equal(1, await CountVisitsAsync(secondCustomer.UserId, hotel.HotelId));
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

    private async Task<Hotel> CreateHotelAsync(
        string? cityName = null,
        string? description = "Description",
        string? history = "History")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var city = new City(cityName ?? Unique("City"), "Palestine", Unique("PO"));
        db.Cities.Add(city);
        await db.SaveChangesAsync();
        var hotel = new Hotel(Unique("Hotel"), "Owner", "Address", 32.2, 35.2,
            HotelType.Luxury, city.CityId, description, history);
        db.Hotels.Add(hotel);
        await db.SaveChangesAsync();
        return hotel;
    }

    private async Task<Room> CreateRoomAsync(int hotelId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var room = new Room(Unique("Room"), RoomType.Double, 100m, 2, 1, hotelId, "Room");
        db.Rooms.Add(room);
        await db.SaveChangesAsync();
        return room;
    }

    private async Task AddAmenitiesAsync(int hotelId, params (string Name, string? Description)[] values)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        foreach (var value in values)
        {
            var amenity = new Amenity(value.Name, value.Description);
            db.Amenities.Add(amenity);
            await db.SaveChangesAsync();
            db.HotelAmenities.Add(new HotelAmenity { HotelId = hotelId, AmenityId = amenity.AmenityId });
        }
        await db.SaveChangesAsync();
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

    private async Task<RecentlyVisitedHotel> CreateVisitAsync(int userId, int hotelId, DateTime visitedAt)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var visit = new RecentlyVisitedHotel { UserId = userId, HotelId = hotelId, VisitedAt = visitedAt };
        db.RecentlyVisitedHotels.Add(visit);
        await db.SaveChangesAsync();
        return visit;
    }

    private async Task<RecentlyVisitedHotel?> FindVisitAsync(int userId, int hotelId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        return await db.RecentlyVisitedHotels.AsNoTracking()
            .SingleOrDefaultAsync(item => item.UserId == userId && item.HotelId == hotelId);
    }

    private async Task<int> CountVisitsAsync(int userId, int hotelId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        return await db.RecentlyVisitedHotels.CountAsync(
            item => item.UserId == userId && item.HotelId == hotelId);
    }

    private async Task UpdateHotelAsync(int hotelId, Action<Hotel> update)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var hotel = await db.Hotels.FindAsync(hotelId);
        Assert.NotNull(hotel);
        update(hotel);
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
