using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HotelBooking.Application.Hotels.Dtos;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.IntegrationTests.Infrastructure;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.IntegrationTests.Hotels;

public class HotelsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public HotelsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData(HotelEndpoint.Get)]
    [InlineData(HotelEndpoint.Create)]
    [InlineData(HotelEndpoint.Update)]
    [InlineData(HotelEndpoint.Status)]
    public async Task HotelEndpoint_WithoutToken_ShouldReturnUnauthorized(HotelEndpoint endpoint)
    {
        using var client = _factory.CreateClient();
        using var response = await SendAsync(client, endpoint, 1, ValidRequest(1));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(HotelEndpoint.Get)]
    [InlineData(HotelEndpoint.Create)]
    [InlineData(HotelEndpoint.Update)]
    [InlineData(HotelEndpoint.Status)]
    public async Task HotelEndpoint_WithCustomerToken_ShouldReturnForbidden(HotelEndpoint endpoint)
    {
        var customer = await CreateUserAsync(Role.Customer);
        using var client = CreateAuthenticatedClient(customer);
        using var response = await SendAsync(client, endpoint, 1, ValidRequest(1));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetHotels_ShouldReturnActiveAndInactiveHotelsWithAllMappedFields()
    {
        var admin = await CreateUserAsync(Role.Admin);
        var city = await CreateCityAsync();
        var active = await CreateHotelAsync(city.CityId, "Mapped Active", "Active Owner", true);
        var inactive = await CreateHotelAsync(city.CityId, "Mapped Inactive", "Inactive Owner", false);
        using var client = CreateAuthenticatedClient(admin);

        var response = await client.GetAsync("/api/Hotels");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<HotelResponseDto>>(JsonOptions);
        Assert.NotNull(result);
        AssertHotel(result.Single(item => item.HotelId == active.HotelId), active);
        AssertHotel(result.Single(item => item.HotelId == inactive.HotelId), inactive);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetHotels_WithSearch_ShouldMatchNameOrOwnerOnly(bool matchOwner)
    {
        var admin = await CreateUserAsync(Role.Admin);
        var city = await CreateCityAsync();
        var token = Unique("SearchToken");
        var matching = await CreateHotelAsync(
            city.CityId,
            matchOwner ? Unique("Hotel") : $"Hotel {token}",
            matchOwner ? $"Owner {token}" : Unique("Owner"));
        await CreateHotelAsync(city.CityId, Unique("UnrelatedHotel"), Unique("UnrelatedOwner"));
        using var client = CreateAuthenticatedClient(admin);

        var response = await client.GetAsync($"/api/Hotels?search={Uri.EscapeDataString(token)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<HotelResponseDto>>(JsonOptions);
        Assert.NotNull(result);
        var hotel = Assert.Single(result);
        Assert.Equal(matching.HotelId, hotel.HotelId);
    }

    [Fact]
    public async Task GetHotels_WhenSearchHasNoMatches_ShouldReturnEmptyArray()
    {
        var admin = await CreateUserAsync(Role.Admin);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.GetAsync($"/api/Hotels?search={Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<HotelResponseDto>>(JsonOptions);
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task CreateHotel_WithValidRequest_ShouldReturnCreatedMappedResponseAndPersistHotel()
    {
        var admin = await CreateUserAsync(Role.Admin);
        var city = await CreateCityAsync();
        using var client = CreateAuthenticatedClient(admin);
        var request = ValidRequest(city.CityId);
        request.Name = "Created Hotel";
        request.OwnerName = "Created Owner";
        request.Description = "  Description  ";
        request.History = "  History  ";

        var response = await client.PostAsJsonAsync("/api/Hotels", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("api/hotels", response.Headers.Location?.OriginalString);
        var result = await response.Content.ReadFromJsonAsync<HotelResponseDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.True(result.HotelId > 0);
        Assert.Equal(city.CityId, result.CityId);
        Assert.Equal("Created Hotel", result.Name);
        Assert.Equal("Created Owner", result.OwnerName);
        Assert.Equal(request.Address, result.Address);
        Assert.Equal(request.Latitude, result.Latitude);
        Assert.Equal(request.Longitude, result.Longitude);
        Assert.Equal(request.HotelType, result.HotelType);
        Assert.Equal("Description", result.Description);
        Assert.Equal("History", result.History);
        Assert.True(result.IsActive);
        var stored = await FindHotelAsync(result.HotelId);
        Assert.NotNull(stored);
        Assert.Equal(result.Name, stored.Name);
        Assert.True(stored.IsActive);
    }

    [Fact]
    public async Task CreateHotel_WhenCityDoesNotExist_ShouldReturnNotFoundAndNotPersistHotel()
    {
        var admin = await CreateUserAsync(Role.Admin);
        using var client = CreateAuthenticatedClient(admin);
        var request = ValidRequest(999999);
        request.Name = Unique("MissingCityHotel");

        var response = await client.PostAsJsonAsync("/api/Hotels", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Null(await FindHotelByNameAsync(request.Name));
    }

    [Theory]
    [InlineData(InvalidHotelRequest.ZeroCity)]
    [InlineData(InvalidHotelRequest.EmptyName)]
    [InlineData(InvalidHotelRequest.WhitespaceName)]
    [InlineData(InvalidHotelRequest.EmptyOwner)]
    [InlineData(InvalidHotelRequest.EmptyAddress)]
    [InlineData(InvalidHotelRequest.LatitudeBelowMinimum)]
    [InlineData(InvalidHotelRequest.LatitudeAboveMaximum)]
    [InlineData(InvalidHotelRequest.LongitudeBelowMinimum)]
    [InlineData(InvalidHotelRequest.LongitudeAboveMaximum)]
    [InlineData(InvalidHotelRequest.InvalidHotelType)]
    public async Task CreateHotel_WithInvalidRequest_ShouldReturnBadRequest(InvalidHotelRequest problem)
    {
        var admin = await CreateUserAsync(Role.Admin);
        var city = await CreateCityAsync();
        var request = ValidRequest(city.CityId);
        MakeInvalid(request, problem);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.PostAsJsonAsync("/api/Hotels", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{invalid-json}")]
    public async Task CreateHotel_WithEmptyOrMalformedBody_ShouldReturnBadRequest(string body)
    {
        var admin = await CreateUserAsync(Role.Admin);
        using var client = CreateAuthenticatedClient(admin);
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/Hotels", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateHotel_WithValidRequest_ShouldReturnMappedResponseAndPersistChanges()
    {
        var admin = await CreateUserAsync(Role.Admin);
        var oldCity = await CreateCityAsync();
        var newCity = await CreateCityAsync();
        var hotel = await CreateHotelAsync(oldCity.CityId, "Old Name", "Old Owner", false);
        var request = ValidRequest(newCity.CityId);
        request.Name = "Updated Name";
        request.OwnerName = "Updated Owner";
        request.Address = "Updated Address";
        request.Description = null;
        request.History = "Updated History";
        using var client = CreateAuthenticatedClient(admin);

        var response = await client.PutAsJsonAsync($"/api/Hotels/{hotel.HotelId}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<HotelResponseDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(hotel.HotelId, result.HotelId);
        Assert.Equal(newCity.CityId, result.CityId);
        Assert.Equal("Updated Name", result.Name);
        Assert.Equal("Updated Owner", result.OwnerName);
        Assert.Equal("Updated Address", result.Address);
        Assert.Null(result.Description);
        Assert.Equal("Updated History", result.History);
        Assert.False(result.IsActive);
        var stored = await FindHotelAsync(hotel.HotelId);
        Assert.NotNull(stored);
        Assert.Equal(newCity.CityId, stored.CityId);
        Assert.Equal("Updated Name", stored.Name);
        Assert.False(stored.IsActive);
        Assert.NotNull(stored.UpdatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(999999)]
    public async Task UpdateHotel_WhenHotelDoesNotExist_ShouldReturnNotFound(int hotelId)
    {
        var admin = await CreateUserAsync(Role.Admin);
        var city = await CreateCityAsync();
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.PutAsJsonAsync($"/api/Hotels/{hotelId}", ValidRequest(city.CityId));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateHotel_WhenCityDoesNotExist_ShouldReturnNotFoundAndPreserveHotel()
    {
        var admin = await CreateUserAsync(Role.Admin);
        var city = await CreateCityAsync();
        var hotel = await CreateHotelAsync(city.CityId, "Original", "Owner");
        var request = ValidRequest(999999);
        request.Name = "Should Not Persist";
        using var client = CreateAuthenticatedClient(admin);

        var response = await client.PutAsJsonAsync($"/api/Hotels/{hotel.HotelId}", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Original", (await FindHotelAsync(hotel.HotelId))!.Name);
    }

    [Theory]
    [InlineData(InvalidHotelRequest.EmptyName)]
    [InlineData(InvalidHotelRequest.EmptyOwner)]
    [InlineData(InvalidHotelRequest.EmptyAddress)]
    [InlineData(InvalidHotelRequest.LatitudeAboveMaximum)]
    [InlineData(InvalidHotelRequest.LongitudeBelowMinimum)]
    [InlineData(InvalidHotelRequest.InvalidHotelType)]
    public async Task UpdateHotel_WithInvalidRequest_ShouldReturnBadRequestAndPreserveHotel(InvalidHotelRequest problem)
    {
        var admin = await CreateUserAsync(Role.Admin);
        var city = await CreateCityAsync();
        var hotel = await CreateHotelAsync(city.CityId, "Original", "Owner");
        var request = ValidRequest(city.CityId);
        MakeInvalid(request, problem);
        using var client = CreateAuthenticatedClient(admin);

        var response = await client.PutAsJsonAsync($"/api/Hotels/{hotel.HotelId}", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Original", (await FindHotelAsync(hotel.HotelId))!.Name);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ChangeStatus_WhenHotelExists_ShouldReturnNoContentAndPersistStatus(bool isActive)
    {
        var admin = await CreateUserAsync(Role.Admin);
        var city = await CreateCityAsync();
        var hotel = await CreateHotelAsync(city.CityId, Unique("Hotel"), "Owner", !isActive);
        using var client = CreateAuthenticatedClient(admin);

        var response = await client.PatchAsJsonAsync(
            $"/api/Hotels/{hotel.HotelId}/status", new ChangeHotelStatusRequest { IsActive = isActive });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var stored = await FindHotelAsync(hotel.HotelId);
        Assert.NotNull(stored);
        Assert.Equal(isActive, stored.IsActive);
        Assert.NotNull(stored.UpdatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(999999)]
    public async Task ChangeStatus_WhenHotelDoesNotExist_ShouldReturnNotFound(int hotelId)
    {
        var admin = await CreateUserAsync(Role.Admin);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.PatchAsJsonAsync(
            $"/api/Hotels/{hotelId}/status", new ChangeHotelStatusRequest { IsActive = false });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(HotelEndpoint.Update, "/api/Hotels/not-a-number")]
    [InlineData(HotelEndpoint.Status, "/api/Hotels/not-a-number/status")]
    public async Task HotelMutation_WithNonNumericHotelId_ShouldReturnBadRequest(
        HotelEndpoint endpoint, string path)
    {
        var admin = await CreateUserAsync(Role.Admin);
        using var client = CreateAuthenticatedClient(admin);
        var response = endpoint == HotelEndpoint.Update
            ? await client.PutAsJsonAsync(path, ValidRequest(1))
            : await client.PatchAsJsonAsync(path, new ChangeHotelStatusRequest());
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(HotelEndpoint.Update)]
    [InlineData(HotelEndpoint.Status)]
    public async Task HotelMutation_WithMalformedBody_ShouldReturnBadRequest(HotelEndpoint endpoint)
    {
        var admin = await CreateUserAsync(Role.Admin);
        using var client = CreateAuthenticatedClient(admin);
        using var content = new StringContent("{invalid-json}", Encoding.UTF8, "application/json");
        var path = endpoint == HotelEndpoint.Update ? "/api/Hotels/1" : "/api/Hotels/1/status";
        using var request = new HttpRequestMessage(
            endpoint == HotelEndpoint.Update ? HttpMethod.Put : HttpMethod.Patch, path) { Content = content };
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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

    private async Task<City> CreateCityAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var city = new City(Unique("City"), "Palestine", Unique("PO"));
        db.Cities.Add(city);
        await db.SaveChangesAsync();
        return city;
    }

    private async Task<Hotel> CreateHotelAsync(
        int cityId, string name, string owner, bool isActive = true)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var hotel = new Hotel(name, owner, "Address", 32.2, 35.2,
            HotelType.Luxury, cityId, "Description", "History");
        if (!isActive) hotel.ChangeStatus(false);
        db.Hotels.Add(hotel);
        await db.SaveChangesAsync();
        return hotel;
    }

    private async Task<Hotel?> FindHotelAsync(int hotelId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        return await db.Hotels.AsNoTracking().SingleOrDefaultAsync(item => item.HotelId == hotelId);
    }

    private async Task<Hotel?> FindHotelByNameAsync(string name)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        return await db.Hotels.AsNoTracking().SingleOrDefaultAsync(item => item.Name == name);
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

    private static HotelRequestDto ValidRequest(int cityId) => new()
    {
        CityId = cityId,
        Name = "Valid Hotel",
        OwnerName = "Valid Owner",
        Address = "Valid Address",
        Latitude = 32.2211,
        Longitude = 35.2544,
        HotelType = HotelType.Luxury,
        Description = "Description",
        History = "History"
    };

    private static void MakeInvalid(HotelRequestDto request, InvalidHotelRequest problem)
    {
        switch (problem)
        {
            case InvalidHotelRequest.ZeroCity: request.CityId = 0; break;
            case InvalidHotelRequest.EmptyName: request.Name = string.Empty; break;
            case InvalidHotelRequest.WhitespaceName: request.Name = "   "; break;
            case InvalidHotelRequest.EmptyOwner: request.OwnerName = string.Empty; break;
            case InvalidHotelRequest.EmptyAddress: request.Address = string.Empty; break;
            case InvalidHotelRequest.LatitudeBelowMinimum: request.Latitude = -90.1; break;
            case InvalidHotelRequest.LatitudeAboveMaximum: request.Latitude = 90.1; break;
            case InvalidHotelRequest.LongitudeBelowMinimum: request.Longitude = -180.1; break;
            case InvalidHotelRequest.LongitudeAboveMaximum: request.Longitude = 180.1; break;
            case InvalidHotelRequest.InvalidHotelType: request.HotelType = (HotelType)999; break;
        }
    }

    private static void AssertHotel(HotelResponseDto actual, Hotel expected)
    {
        Assert.Equal(expected.HotelId, actual.HotelId);
        Assert.Equal(expected.CityId, actual.CityId);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.OwnerName, actual.OwnerName);
        Assert.Equal(expected.Address, actual.Address);
        Assert.Equal(expected.Latitude, actual.Latitude);
        Assert.Equal(expected.Longitude, actual.Longitude);
        Assert.Equal(expected.HotelType, actual.HotelType);
        Assert.Equal(expected.Description, actual.Description);
        Assert.Equal(expected.History, actual.History);
        Assert.Equal(expected.IsActive, actual.IsActive);
    }

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client, HotelEndpoint endpoint, int hotelId, HotelRequestDto request) => endpoint switch
    {
        HotelEndpoint.Get => await client.GetAsync("/api/Hotels"),
        HotelEndpoint.Create => await client.PostAsJsonAsync("/api/Hotels", request),
        HotelEndpoint.Update => await client.PutAsJsonAsync($"/api/Hotels/{hotelId}", request),
        _ => await client.PatchAsJsonAsync(
            $"/api/Hotels/{hotelId}/status", new ChangeHotelStatusRequest { IsActive = false })
    };

    private static string Unique(string prefix) => $"{prefix}_{Guid.NewGuid():N}";

    public enum HotelEndpoint { Get, Create, Update, Status }
    public enum InvalidHotelRequest
    {
        ZeroCity,
        EmptyName,
        WhitespaceName,
        EmptyOwner,
        EmptyAddress,
        LatitudeBelowMinimum,
        LatitudeAboveMaximum,
        LongitudeBelowMinimum,
        LongitudeAboveMaximum,
        InvalidHotelType
    }
}
