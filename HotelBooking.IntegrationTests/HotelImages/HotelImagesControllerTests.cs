using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HotelBooking.Application.HotelImages.Dtos;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.IntegrationTests.Infrastructure;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.IntegrationTests.HotelImages;

public class HotelImagesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public HotelImagesControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHotelImages_WithoutToken_ShouldReturnUnauthorized()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/hotels/1/images");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetHotelImages_WithAdminToken_ShouldReturnForbidden()
    {
        var admin = await CreateUserAsync(Role.Admin);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.GetAsync("/api/hotels/1/images");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetHotelImages_WithNonNumericHotelId_ShouldReturnBadRequest()
    {
        var customer = await CreateUserAsync(Role.Customer);
        using var client = CreateAuthenticatedClient(customer);
        var response = await client.GetAsync("/api/hotels/not-a-number/images");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(999999)]
    public async Task GetHotelImages_WhenHotelDoesNotExist_ShouldReturnNotFound(int hotelId)
    {
        var customer = await CreateUserAsync(Role.Customer);
        using var client = CreateAuthenticatedClient(customer);
        var response = await client.GetAsync($"/api/hotels/{hotelId}/images");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetHotelImages_WhenHotelIsInactive_ShouldReturnNotFound()
    {
        var customer = await CreateUserAsync(Role.Customer);
        var hotel = await CreateHotelAsync();
        await SetHotelActiveAsync(hotel.HotelId, false);
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync($"/api/hotels/{hotel.HotelId}/images");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetHotelImages_WhenHotelHasNoImages_ShouldReturnEmptyArray()
    {
        var customer = await CreateUserAsync(Role.Customer);
        var hotel = await CreateHotelAsync();
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync($"/api/hotels/{hotel.HotelId}/images");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<HotelImageResponseDto>>();
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetHotelImages_ShouldReturnMappedImagesOrderedByDisplayOrderForRequestedHotelOnly()
    {
        var customer = await CreateUserAsync(Role.Customer);
        var hotel = await CreateHotelAsync();
        var otherHotel = await CreateHotelAsync();
        await AddImagesAsync(hotel.HotelId,
            ("third.jpg", 3),
            ("first.jpg", 1),
            ("second.jpg", 2));
        await AddImagesAsync(otherHotel.HotelId, ("other.jpg", 1));
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync($"/api/hotels/{hotel.HotelId}/images");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<HotelImageResponseDto>>();
        Assert.NotNull(result);
        Assert.Collection(result,
            image => { Assert.Equal("first.jpg", image.ImageUrl); Assert.Equal(1, image.DisplayOrder); },
            image => { Assert.Equal("second.jpg", image.ImageUrl); Assert.Equal(2, image.DisplayOrder); },
            image => { Assert.Equal("third.jpg", image.ImageUrl); Assert.Equal(3, image.DisplayOrder); });
        Assert.DoesNotContain(result, image => image.ImageUrl == "other.jpg");
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

    private async Task<Hotel> CreateHotelAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var city = new City(Unique("City"), "Palestine", Unique("PO"));
        db.Cities.Add(city);
        await db.SaveChangesAsync();
        var hotel = new Hotel(Unique("Hotel"), "Owner", "Address", 32.2, 35.2,
            HotelType.Luxury, city.CityId, "Description", "History");
        db.Hotels.Add(hotel);
        await db.SaveChangesAsync();
        return hotel;
    }

    private async Task AddImagesAsync(int hotelId, params (string Url, int Order)[] images)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        db.HotelImages.AddRange(images.Select(image => new HotelImage(image.Url, image.Order, hotelId)));
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
