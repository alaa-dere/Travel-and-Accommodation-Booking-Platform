using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HotelBooking.Application.HotelLocations.Dtos;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.IntegrationTests.Infrastructure;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.IntegrationTests.HotelLocations;

public class HotelLocationControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public HotelLocationControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHotelLocation_WithoutToken_ShouldReturnUnauthorized()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/hotels/1/location");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetHotelLocation_WithAdminToken_ShouldReturnForbidden()
    {
        var admin = await CreateUserAsync(Role.Admin);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.GetAsync("/api/hotels/1/location");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetHotelLocation_WithNonNumericHotelId_ShouldReturnBadRequest()
    {
        var customer = await CreateUserAsync(Role.Customer);
        using var client = CreateAuthenticatedClient(customer);
        var response = await client.GetAsync("/api/hotels/not-a-number/location");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(999999)]
    public async Task GetHotelLocation_WhenHotelDoesNotExist_ShouldReturnNotFound(int hotelId)
    {
        var customer = await CreateUserAsync(Role.Customer);
        using var client = CreateAuthenticatedClient(customer);
        var response = await client.GetAsync($"/api/hotels/{hotelId}/location");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetHotelLocation_WhenHotelIsInactive_ShouldReturnNotFound()
    {
        var customer = await CreateUserAsync(Role.Customer);
        var hotel = await CreateHotelAsync();
        await SetHotelActiveAsync(hotel.HotelId, false);
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync($"/api/hotels/{hotel.HotelId}/location");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetHotelLocation_WithoutAttractions_ShouldReturnCoordinatesAndEmptyArray()
    {
        var customer = await CreateUserAsync(Role.Customer);
        var hotel = await CreateHotelAsync(31.9522, 35.2332);
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync($"/api/hotels/{hotel.HotelId}/location");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<HotelLocationResponseDto>();
        Assert.NotNull(result);
        Assert.Equal(31.9522, result.Latitude);
        Assert.Equal(35.2332, result.Longitude);
        Assert.Empty(result.Attractions);
    }

    [Fact]
    public async Task GetHotelLocation_ShouldMapAllAttractionsForRequestedHotelOnly()
    {
        var customer = await CreateUserAsync(Role.Customer);
        var hotel = await CreateHotelAsync(32.2211, 35.2544);
        var otherHotel = await CreateHotelAsync();
        await AddAttractionsAsync(hotel.HotelId,
            ("Museum", "Local history", 32.22, 35.25),
            ("Park", null, 32.23, 35.26));
        await AddAttractionsAsync(otherHotel.HotelId,
            ("Other attraction", "Must not leak", 31.0, 34.0));
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync($"/api/hotels/{hotel.HotelId}/location");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<HotelLocationResponseDto>();
        Assert.NotNull(result);
        Assert.Equal(hotel.Latitude, result.Latitude);
        Assert.Equal(hotel.Longitude, result.Longitude);
        Assert.Equal(2, result.Attractions.Count);
        Assert.Contains(result.Attractions, attraction =>
            attraction.Name == "Museum" &&
            attraction.Description == "Local history" &&
            attraction.Latitude == 32.22 &&
            attraction.Longitude == 35.25);
        Assert.Contains(result.Attractions, attraction =>
            attraction.Name == "Park" &&
            attraction.Description == null &&
            attraction.Latitude == 32.23 &&
            attraction.Longitude == 35.26);
        Assert.DoesNotContain(result.Attractions, attraction => attraction.Name == "Other attraction");
    }

    [Fact]
    public async Task GetHotelLocation_ShouldPreserveBoundaryCoordinates()
    {
        var customer = await CreateUserAsync(Role.Customer);
        var hotel = await CreateHotelAsync(-90, 180);
        await AddAttractionsAsync(hotel.HotelId,
            ("Boundary", "Edge coordinates", 90, -180));
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync($"/api/hotels/{hotel.HotelId}/location");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<HotelLocationResponseDto>();
        Assert.NotNull(result);
        Assert.Equal(-90, result.Latitude);
        Assert.Equal(180, result.Longitude);
        var attraction = Assert.Single(result.Attractions);
        Assert.Equal(90, attraction.Latitude);
        Assert.Equal(-180, attraction.Longitude);
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

    private async Task<Hotel> CreateHotelAsync(double latitude = 32.2, double longitude = 35.2)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var city = new City(Unique("City"), "Palestine", Unique("PO"));
        db.Cities.Add(city);
        await db.SaveChangesAsync();
        var hotel = new Hotel(Unique("Hotel"), "Owner", "Address", latitude, longitude,
            HotelType.Luxury, city.CityId, "Description", "History");
        db.Hotels.Add(hotel);
        await db.SaveChangesAsync();
        return hotel;
    }

    private async Task AddAttractionsAsync(
        int hotelId,
        params (string Name, string? Description, double Latitude, double Longitude)[] attractions)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        db.NearbyAttractions.AddRange(attractions.Select(attraction =>
            new NearbyAttraction(
                hotelId,
                attraction.Name,
                attraction.Description,
                attraction.Latitude,
                attraction.Longitude)));
        await db.SaveChangesAsync();
    }

    private async Task SetHotelActiveAsync(int hotelId, bool isActive)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var hotel = await db.Hotels.FindAsync(hotelId);
        Assert.NotNull(hotel);
        hotel.ChangeStatus(isActive);
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
