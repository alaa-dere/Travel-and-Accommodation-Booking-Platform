using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.NearbyAttractions.Dtos;
using HotelBooking.Application.NearbyAttractions.Update;
using HotelBooking.Domain.Entities;
using HotelBooking.IntegrationTests.Infrastructure;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.IntegrationTests.NearbyAttractions;

public class NearbyAttractionsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public NearbyAttractionsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData(AttractionEndpoint.Create)]
    [InlineData(AttractionEndpoint.Get)]
    [InlineData(AttractionEndpoint.Update)]
    [InlineData(AttractionEndpoint.Remove)]
    public async Task AttractionEndpoint_WithoutToken_ShouldReturnUnauthorized(AttractionEndpoint endpoint)
    {
        using var client = _factory.CreateClient();
        using var response = await SendAsync(client, endpoint, 1, ValidCreateRequest(1));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(AttractionEndpoint.Create)]
    [InlineData(AttractionEndpoint.Get)]
    [InlineData(AttractionEndpoint.Update)]
    [InlineData(AttractionEndpoint.Remove)]
    public async Task AttractionEndpoint_WithCustomerToken_ShouldReturnForbidden(AttractionEndpoint endpoint)
    {
        var customer = await CreateUserAsync(Role.Customer);
        using var client = CreateAuthenticatedClient(customer);
        using var response = await SendAsync(client, endpoint, 1, ValidCreateRequest(1));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithValidRequest_ShouldReturnCreatedAndPersistTrimmedValues()
    {
        var admin = await CreateUserAsync(Role.Admin);
        var hotel = await CreateHotelAsync();
        using var client = CreateAuthenticatedClient(admin);
        var request = ValidCreateRequest(hotel.HotelId);
        request.Name = "  Museum  ";
        request.Description = "  Local history  ";

        var response = await client.PostAsJsonAsync("/api/nearby-attractions", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var attraction = await db.NearbyAttractions.AsNoTracking()
            .SingleAsync(item => item.HotelId == hotel.HotelId);
        Assert.Equal("Museum", attraction.Name);
        Assert.Equal("Local history", attraction.Description);
        Assert.Equal(request.Latitude, attraction.Latitude);
        Assert.Equal(request.Longitude, attraction.Longitude);
    }

    [Fact]
    public async Task Create_ForInactiveHotel_ShouldSucceed()
    {
        var admin = await CreateUserAsync(Role.Admin);
        var hotel = await CreateHotelAsync(false);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.PostAsJsonAsync(
            "/api/nearby-attractions", ValidCreateRequest(hotel.HotelId));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_WhenHotelDoesNotExist_ShouldReturnNotFoundAndNotPersist()
    {
        var admin = await CreateUserAsync(Role.Admin);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.PostAsJsonAsync(
            "/api/nearby-attractions", ValidCreateRequest(999999));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(InvalidAttractionRequest.ZeroHotelId)]
    [InlineData(InvalidAttractionRequest.EmptyName)]
    [InlineData(InvalidAttractionRequest.WhitespaceName)]
    [InlineData(InvalidAttractionRequest.NameTooLong)]
    [InlineData(InvalidAttractionRequest.DescriptionTooLong)]
    [InlineData(InvalidAttractionRequest.LatitudeBelowMinimum)]
    [InlineData(InvalidAttractionRequest.LatitudeAboveMaximum)]
    [InlineData(InvalidAttractionRequest.LongitudeBelowMinimum)]
    [InlineData(InvalidAttractionRequest.LongitudeAboveMaximum)]
    public async Task Create_WithInvalidRequest_ShouldReturnBadRequest(InvalidAttractionRequest problem)
    {
        var admin = await CreateUserAsync(Role.Admin);
        var hotel = await CreateHotelAsync();
        var request = ValidCreateRequest(hotel.HotelId);
        MakeInvalid(request, problem);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.PostAsJsonAsync("/api/nearby-attractions", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithMaximumLengthsAndBoundaryCoordinates_ShouldSucceed()
    {
        var admin = await CreateUserAsync(Role.Admin);
        var hotel = await CreateHotelAsync();
        var request = ValidCreateRequest(hotel.HotelId);
        request.Name = new string('n', 100);
        request.Description = new string('d', 500);
        request.Latitude = -90;
        request.Longitude = 180;
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.PostAsJsonAsync("/api/nearby-attractions", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{invalid-json}")]
    public async Task Create_WithEmptyOrMalformedBody_ShouldReturnBadRequest(string body)
    {
        var admin = await CreateUserAsync(Role.Admin);
        using var client = CreateAuthenticatedClient(admin);
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/nearby-attractions", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task GetByHotel_WithInvalidHotelId_ShouldReturnBadRequest(int hotelId)
    {
        var admin = await CreateUserAsync(Role.Admin);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.GetAsync($"/api/nearby-attractions/hotel/{hotelId}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetByHotel_WhenHotelDoesNotExist_ShouldReturnNotFound()
    {
        var admin = await CreateUserAsync(Role.Admin);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.GetAsync("/api/nearby-attractions/hotel/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetByHotel_WhenNoAttractions_ShouldReturnEmptyArray()
    {
        var admin = await CreateUserAsync(Role.Admin);
        var hotel = await CreateHotelAsync();
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.GetAsync($"/api/nearby-attractions/hotel/{hotel.HotelId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<NearbyAttractionResponseDto>>();
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByHotel_ShouldMapRequestedHotelsAttractionsInNameOrder()
    {
        var admin = await CreateUserAsync(Role.Admin);
        var hotel = await CreateHotelAsync();
        var otherHotel = await CreateHotelAsync();
        var zoo = await CreateAttractionAsync(hotel.HotelId, "Zoo", null, 32.2, 35.2);
        var aquarium = await CreateAttractionAsync(hotel.HotelId, "Aquarium", "Fish", 32.3, 35.3);
        await CreateAttractionAsync(otherHotel.HotelId, "Other", "Excluded", 31, 34);
        using var client = CreateAuthenticatedClient(admin);

        var response = await client.GetAsync($"/api/nearby-attractions/hotel/{hotel.HotelId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<NearbyAttractionResponseDto>>();
        Assert.NotNull(result);
        Assert.Collection(result,
            item => AssertAttraction(item, aquarium),
            item => AssertAttraction(item, zoo));
    }

    [Fact]
    public async Task GetByHotel_ForInactiveHotel_ShouldReturnAttractions()
    {
        var admin = await CreateUserAsync(Role.Admin);
        var hotel = await CreateHotelAsync(false);
        var attraction = await CreateAttractionAsync(hotel.HotelId, "Museum", null, 32, 35);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.GetAsync($"/api/nearby-attractions/hotel/{hotel.HotelId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<NearbyAttractionResponseDto>>();
        Assert.NotNull(result);
        Assert.Equal(attraction.NearbyAttractionId, Assert.Single(result).NearbyAttractionId);
    }

    [Fact]
    public async Task Update_WithValidRequest_ShouldReturnNoContentAndPersistTrimmedValues()
    {
        var admin = await CreateUserAsync(Role.Admin);
        var hotel = await CreateHotelAsync();
        var attraction = await CreateAttractionAsync(hotel.HotelId, "Old", "Old", 31, 34);
        var request = ValidUpdateRequest();
        request.Name = "  Updated  ";
        request.Description = "  Updated description  ";
        using var client = CreateAuthenticatedClient(admin);

        var response = await client.PutAsJsonAsync(
            $"/api/nearby-attractions/{attraction.NearbyAttractionId}", request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var stored = await FindAttractionAsync(attraction.NearbyAttractionId);
        Assert.NotNull(stored);
        Assert.Equal("Updated", stored.Name);
        Assert.Equal("Updated description", stored.Description);
        Assert.Equal(request.Latitude, stored.Latitude);
        Assert.Equal(request.Longitude, stored.Longitude);
        Assert.Equal(hotel.HotelId, stored.HotelId);
    }

    [Theory]
    [InlineData(-1, HttpStatusCode.BadRequest)]
    [InlineData(0, HttpStatusCode.BadRequest)]
    [InlineData(999999, HttpStatusCode.NotFound)]
    public async Task Update_WithInvalidOrMissingAttractionId_ShouldReturnExpectedStatus(
        int attractionId, HttpStatusCode expectedStatus)
    {
        var admin = await CreateUserAsync(Role.Admin);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.PutAsJsonAsync(
            $"/api/nearby-attractions/{attractionId}", ValidUpdateRequest());
        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Theory]
    [InlineData(InvalidAttractionRequest.EmptyName)]
    [InlineData(InvalidAttractionRequest.WhitespaceName)]
    [InlineData(InvalidAttractionRequest.NameTooLong)]
    [InlineData(InvalidAttractionRequest.DescriptionTooLong)]
    [InlineData(InvalidAttractionRequest.LatitudeBelowMinimum)]
    [InlineData(InvalidAttractionRequest.LatitudeAboveMaximum)]
    [InlineData(InvalidAttractionRequest.LongitudeBelowMinimum)]
    [InlineData(InvalidAttractionRequest.LongitudeAboveMaximum)]
    public async Task Update_WithInvalidRequest_ShouldReturnBadRequestAndPreserveAttraction(
        InvalidAttractionRequest problem)
    {
        var admin = await CreateUserAsync(Role.Admin);
        var hotel = await CreateHotelAsync();
        var attraction = await CreateAttractionAsync(hotel.HotelId, "Original", "Original", 31, 34);
        var request = ValidUpdateRequest();
        MakeInvalid(request, problem);
        using var client = CreateAuthenticatedClient(admin);

        var response = await client.PutAsJsonAsync(
            $"/api/nearby-attractions/{attraction.NearbyAttractionId}", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Original", (await FindAttractionAsync(attraction.NearbyAttractionId))!.Name);
    }

    [Fact]
    public async Task Update_WithMaximumLengthsAndBoundaryCoordinates_ShouldSucceed()
    {
        var admin = await CreateUserAsync(Role.Admin);
        var hotel = await CreateHotelAsync();
        var attraction = await CreateAttractionAsync(hotel.HotelId, "Original", null, 31, 34);
        var request = ValidUpdateRequest();
        request.Name = new string('n', 100);
        request.Description = new string('d', 500);
        request.Latitude = 90;
        request.Longitude = -180;
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.PutAsJsonAsync(
            $"/api/nearby-attractions/{attraction.NearbyAttractionId}", request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{invalid-json}")]
    public async Task Update_WithEmptyOrMalformedBody_ShouldReturnBadRequest(string body)
    {
        var admin = await CreateUserAsync(Role.Admin);
        using var client = CreateAuthenticatedClient(admin);
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await client.PutAsync("/api/nearby-attractions/1", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Remove_WhenAttractionExists_ShouldReturnNoContentAndDeleteAttraction()
    {
        var admin = await CreateUserAsync(Role.Admin);
        var hotel = await CreateHotelAsync();
        var attraction = await CreateAttractionAsync(hotel.HotelId, "Delete me", null, 31, 34);
        using var client = CreateAuthenticatedClient(admin);

        var response = await client.DeleteAsync(
            $"/api/nearby-attractions/{attraction.NearbyAttractionId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(await FindAttractionAsync(attraction.NearbyAttractionId));
    }

    [Theory]
    [InlineData(-1, HttpStatusCode.BadRequest)]
    [InlineData(0, HttpStatusCode.BadRequest)]
    [InlineData(999999, HttpStatusCode.NotFound)]
    public async Task Remove_WithInvalidOrMissingAttractionId_ShouldReturnExpectedStatus(
        int attractionId, HttpStatusCode expectedStatus)
    {
        var admin = await CreateUserAsync(Role.Admin);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.DeleteAsync($"/api/nearby-attractions/{attractionId}");
        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/nearby-attractions/hotel/not-a-number", HttpStatusCode.NotFound)]
    [InlineData("/api/nearby-attractions/not-a-number", HttpStatusCode.MethodNotAllowed)]
    public async Task GetAttractionRoute_WithNonNumericId_ShouldReturnRoutingStatus(
        string path, HttpStatusCode expectedStatus)
    {
        var admin = await CreateUserAsync(Role.Admin);
        using var client = CreateAuthenticatedClient(admin);
        var getResponse = await client.GetAsync(path);
        Assert.Equal(expectedStatus, getResponse.StatusCode);
    }

    [Fact]
    public async Task Update_WithNonNumericAttractionId_ShouldReturnNotFound()
    {
        var admin = await CreateUserAsync(Role.Admin);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.PutAsJsonAsync(
            "/api/nearby-attractions/not-a-number", ValidUpdateRequest());
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Remove_WithNonNumericAttractionId_ShouldReturnNotFound()
    {
        var admin = await CreateUserAsync(Role.Admin);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.DeleteAsync("/api/nearby-attractions/not-a-number");
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

    private async Task<NearbyAttraction> CreateAttractionAsync(
        int hotelId, string name, string? description, double latitude, double longitude)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var attraction = new NearbyAttraction(hotelId, name, description, latitude, longitude);
        db.NearbyAttractions.Add(attraction);
        await db.SaveChangesAsync();
        return attraction;
    }

    private async Task<NearbyAttraction?> FindAttractionAsync(int attractionId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        return await db.NearbyAttractions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.NearbyAttractionId == attractionId);
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

    private static CreateNearbyAttractionRequestDto ValidCreateRequest(int hotelId) => new()
    {
        HotelId = hotelId,
        Name = "Museum",
        Description = "Local history",
        Latitude = 32.22,
        Longitude = 35.25
    };

    private static UpdateNearbyAttractionRequestDto ValidUpdateRequest() => new()
    {
        Name = "Updated attraction",
        Description = "Updated description",
        Latitude = 32.23,
        Longitude = 35.26
    };

    private static void MakeInvalid(CreateNearbyAttractionRequestDto request, InvalidAttractionRequest problem)
    {
        if (problem == InvalidAttractionRequest.ZeroHotelId) request.HotelId = 0;
        ApplyInvalid(problem, value => request.Name = value, value => request.Description = value,
            value => request.Latitude = value, value => request.Longitude = value);
    }

    private static void MakeInvalid(UpdateNearbyAttractionRequestDto request, InvalidAttractionRequest problem)
    {
        ApplyInvalid(problem, value => request.Name = value, value => request.Description = value,
            value => request.Latitude = value, value => request.Longitude = value);
    }

    private static void ApplyInvalid(
        InvalidAttractionRequest problem,
        Action<string> setName,
        Action<string?> setDescription,
        Action<double> setLatitude,
        Action<double> setLongitude)
    {
        switch (problem)
        {
            case InvalidAttractionRequest.EmptyName: setName(string.Empty); break;
            case InvalidAttractionRequest.WhitespaceName: setName("   "); break;
            case InvalidAttractionRequest.NameTooLong: setName(new string('n', 101)); break;
            case InvalidAttractionRequest.DescriptionTooLong: setDescription(new string('d', 501)); break;
            case InvalidAttractionRequest.LatitudeBelowMinimum: setLatitude(-90.1); break;
            case InvalidAttractionRequest.LatitudeAboveMaximum: setLatitude(90.1); break;
            case InvalidAttractionRequest.LongitudeBelowMinimum: setLongitude(-180.1); break;
            case InvalidAttractionRequest.LongitudeAboveMaximum: setLongitude(180.1); break;
        }
    }

    private static void AssertAttraction(NearbyAttractionResponseDto actual, NearbyAttraction expected)
    {
        Assert.Equal(expected.NearbyAttractionId, actual.NearbyAttractionId);
        Assert.Equal(expected.HotelId, actual.HotelId);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Description, actual.Description);
        Assert.Equal(expected.Latitude, actual.Latitude);
        Assert.Equal(expected.Longitude, actual.Longitude);
    }

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        AttractionEndpoint endpoint,
        int id,
        CreateNearbyAttractionRequestDto request) => endpoint switch
    {
        AttractionEndpoint.Create => await client.PostAsJsonAsync("/api/nearby-attractions", request),
        AttractionEndpoint.Get => await client.GetAsync($"/api/nearby-attractions/hotel/{id}"),
        AttractionEndpoint.Update => await client.PutAsJsonAsync(
            $"/api/nearby-attractions/{id}", ValidUpdateRequest()),
        _ => await client.DeleteAsync($"/api/nearby-attractions/{id}")
    };

    private static string Unique(string prefix) => $"{prefix}_{Guid.NewGuid():N}";

    public enum AttractionEndpoint { Create, Get, Update, Remove }
    public enum InvalidAttractionRequest
    {
        ZeroHotelId,
        EmptyName,
        WhitespaceName,
        NameTooLong,
        DescriptionTooLong,
        LatitudeBelowMinimum,
        LatitudeAboveMaximum,
        LongitudeBelowMinimum,
        LongitudeAboveMaximum
    }
}
