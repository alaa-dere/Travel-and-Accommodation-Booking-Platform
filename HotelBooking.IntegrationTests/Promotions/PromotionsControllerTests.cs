using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Promotions.Create;
using HotelBooking.Domain.Entities;
using HotelBooking.IntegrationTests.Infrastructure;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.IntegrationTests.Promotions;

public class PromotionsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PromotionsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData(PromotionEndpoint.Create)]
    [InlineData(PromotionEndpoint.Status)]
    public async Task PromotionEndpoint_WithoutToken_ShouldReturnUnauthorized(PromotionEndpoint endpoint)
    {
        using var client = _factory.CreateClient();
        using var response = await SendAsync(client, endpoint, 1, ValidRequest(1));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(PromotionEndpoint.Create)]
    [InlineData(PromotionEndpoint.Status)]
    public async Task PromotionEndpoint_WithCustomerToken_ShouldReturnForbidden(PromotionEndpoint endpoint)
    {
        var customer = await CreateUserAsync(Role.Customer);
        using var client = CreateAuthenticatedClient(customer);
        using var response = await SendAsync(client, endpoint, 1, ValidRequest(1));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreatePromotion_WithValidRequest_ShouldReturnCreatedAndPersistActivePromotion()
    {
        var admin = await CreateUserAsync(Role.Admin);
        var hotel = await CreateHotelAsync();
        var request = ValidRequest(hotel.HotelId);
        using var client = CreateAuthenticatedClient(admin);

        var response = await client.PostAsJsonAsync("/api/promotions", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var promotion = await db.Promotions.AsNoTracking()
            .SingleAsync(item => item.HotelId == hotel.HotelId);
        Assert.Equal(request.DiscountPercentage, promotion.DiscountPercentage);
        Assert.Equal(request.StartDate, promotion.StartDate);
        Assert.Equal(request.EndDate, promotion.EndDate);
        Assert.True(promotion.IsActive);
        Assert.True(promotion.CreatedAt <= DateTime.UtcNow);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(99)]
    public async Task CreatePromotion_WithBoundaryDiscount_ShouldSucceed(int discount)
    {
        var admin = await CreateUserAsync(Role.Admin);
        var hotel = await CreateHotelAsync();
        var request = ValidRequest(hotel.HotelId);
        request.DiscountPercentage = discount;
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.PostAsJsonAsync("/api/promotions", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreatePromotion_ForInactiveHotel_ShouldSucceed()
    {
        var admin = await CreateUserAsync(Role.Admin);
        var hotel = await CreateHotelAsync(false);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.PostAsJsonAsync("/api/promotions", ValidRequest(hotel.HotelId));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task CreatePromotion_WithInvalidHotelId_ShouldReturnBadRequest(int hotelId)
    {
        var admin = await CreateUserAsync(Role.Admin);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.PostAsJsonAsync("/api/promotions", ValidRequest(hotelId));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(101)]
    public async Task CreatePromotion_WithInvalidDiscount_ShouldReturnBadRequest(int discount)
    {
        var admin = await CreateUserAsync(Role.Admin);
        var hotel = await CreateHotelAsync();
        var request = ValidRequest(hotel.HotelId);
        request.DiscountPercentage = discount;
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.PostAsJsonAsync("/api/promotions", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CreatePromotion_WhenEndDateIsNotAfterStartDate_ShouldReturnBadRequest(bool equal)
    {
        var admin = await CreateUserAsync(Role.Admin);
        var hotel = await CreateHotelAsync();
        var request = ValidRequest(hotel.HotelId);
        request.EndDate = equal ? request.StartDate : request.StartDate.AddDays(-1);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.PostAsJsonAsync("/api/promotions", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreatePromotion_WhenHotelDoesNotExist_ShouldReturnNotFoundAndNotPersist()
    {
        var admin = await CreateUserAsync(Role.Admin);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.PostAsJsonAsync("/api/promotions", ValidRequest(999999));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{invalid-json}")]
    public async Task CreatePromotion_WithEmptyOrMalformedBody_ShouldReturnBadRequest(string body)
    {
        var admin = await CreateUserAsync(Role.Admin);
        using var client = CreateAuthenticatedClient(admin);
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/promotions", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreatePromotion_ShouldAllowMultiplePromotionsForSameHotel()
    {
        var admin = await CreateUserAsync(Role.Admin);
        var hotel = await CreateHotelAsync();
        using var client = CreateAuthenticatedClient(admin);
        var first = ValidRequest(hotel.HotelId);
        var second = ValidRequest(hotel.HotelId);
        second.DiscountPercentage = 30;

        Assert.Equal(HttpStatusCode.Created,
            (await client.PostAsJsonAsync("/api/promotions", first)).StatusCode);
        Assert.Equal(HttpStatusCode.Created,
            (await client.PostAsJsonAsync("/api/promotions", second)).StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        Assert.Equal(2, await db.Promotions.CountAsync(item => item.HotelId == hotel.HotelId));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ChangeStatus_WhenPromotionExists_ShouldReturnNoContentAndPersistStatus(bool isActive)
    {
        var admin = await CreateUserAsync(Role.Admin);
        var hotel = await CreateHotelAsync();
        var promotion = await CreatePromotionAsync(hotel.HotelId, !isActive);
        using var client = CreateAuthenticatedClient(admin);

        var response = await client.PatchAsync(
            $"/api/promotions/{promotion.PromotionId}/status?isActive={isActive}", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(isActive, (await FindPromotionAsync(promotion.PromotionId))!.IsActive);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task ChangeStatus_WithInvalidPromotionId_ShouldReturnBadRequest(int promotionId)
    {
        var admin = await CreateUserAsync(Role.Admin);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.PatchAsync(
            $"/api/promotions/{promotionId}/status?isActive=true", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangeStatus_WhenPromotionDoesNotExist_ShouldReturnNotFound()
    {
        var admin = await CreateUserAsync(Role.Admin);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.PatchAsync(
            "/api/promotions/999999/status?isActive=true", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ChangeStatus_WhenIsActiveIsMissing_ShouldReturnBadRequestAndPreserveStatus()
    {
        var admin = await CreateUserAsync(Role.Admin);
        var hotel = await CreateHotelAsync();
        var promotion = await CreatePromotionAsync(hotel.HotelId, true);
        using var client = CreateAuthenticatedClient(admin);

        var response = await client.PatchAsync(
            $"/api/promotions/{promotion.PromotionId}/status", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await FindPromotionAsync(promotion.PromotionId))!.IsActive);
    }

    [Fact]
    public async Task ChangeStatus_WhenIsActiveIsMalformed_ShouldReturnBadRequestAndPreserveStatus()
    {
        var admin = await CreateUserAsync(Role.Admin);
        var hotel = await CreateHotelAsync();
        var promotion = await CreatePromotionAsync(hotel.HotelId, true);
        using var client = CreateAuthenticatedClient(admin);

        var response = await client.PatchAsync(
            $"/api/promotions/{promotion.PromotionId}/status?isActive=not-a-bool", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await FindPromotionAsync(promotion.PromotionId))!.IsActive);
    }

    [Fact]
    public async Task ChangeStatus_WithNonNumericPromotionId_ShouldReturnNotFound()
    {
        var admin = await CreateUserAsync(Role.Admin);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.PatchAsync(
            "/api/promotions/not-a-number/status?isActive=true", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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

    private async Task<Hotel> CreateHotelAsync(bool isActive = true)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var city = new City(Unique("City"), "Palestine", Unique("PO"));
        db.Cities.Add(city);
        await db.SaveChangesAsync();
        var hotel = new Hotel(Unique("Hotel"), "Owner", "Address", 32.2, 35.2,
            HotelType.Luxury, city.CityId, "Description", "History");
        if (!isActive) hotel.ChangeStatus(false);
        db.Hotels.Add(hotel);
        await db.SaveChangesAsync();
        return hotel;
    }

    private async Task<Promotion> CreatePromotionAsync(int hotelId, bool isActive)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var promotion = new Promotion(hotelId, 20,
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));
        if (!isActive) promotion.Deactivate();
        db.Promotions.Add(promotion);
        await db.SaveChangesAsync();
        return promotion;
    }

    private async Task<Promotion?> FindPromotionAsync(int promotionId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        return await db.Promotions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.PromotionId == promotionId);
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

    private static CreatePromotionRequestDto ValidRequest(int hotelId) => new()
    {
        HotelId = hotelId,
        DiscountPercentage = 20,
        StartDate = new DateTime(2030, 1, 1),
        EndDate = new DateTime(2030, 1, 31)
    };

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        PromotionEndpoint endpoint,
        int promotionId,
        CreatePromotionRequestDto request) => endpoint switch
    {
        PromotionEndpoint.Create => await client.PostAsJsonAsync("/api/promotions", request),
        _ => await client.PatchAsync(
            $"/api/promotions/{promotionId}/status?isActive=false", null)
    };

    private static string Unique(string prefix) => $"{prefix}_{Guid.NewGuid():N}";

    public enum PromotionEndpoint { Create, Status }
}
