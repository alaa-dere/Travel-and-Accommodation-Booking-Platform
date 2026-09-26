using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HotelBooking.Application.Cities;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.IntegrationTests.Infrastructure;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.IntegrationTests.Cities;

public class CitiesControllerTests :
    IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CitiesControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCities_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/Cities");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCities_WithCustomerToken_ShouldReturnForbidden()
    {
        // Arrange
        var customer = await CreateUserAsync(
            "customer_cities",
            "customer_cities@test.com",
            Role.Customer);

        using var client = CreateAuthenticatedClient(customer);

        // Act
        var response = await client.GetAsync("/api/Cities");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetCities_WithAdminToken_ShouldReturnOk()
    {
        // Arrange
        var admin = await CreateUserAsync(
            "admin_get_cities",
            "admin_get_cities@test.com",
            Role.Admin);

        using var client = CreateAuthenticatedClient(admin);

        // Act
        var response = await client.GetAsync("/api/Cities");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateCity_WhenRequestIsValid_ShouldReturnCreatedAndSaveCity()
    {
        // Arrange
        var admin = await CreateUserAsync(
            "admin_create_city",
            "admin_create_city@test.com",
            Role.Admin);

        using var client = CreateAuthenticatedClient(admin);

        var request = new CityRequestDto
        {
            Name = "Nablus",
            Country = "Palestine",
            PostOffice = "P100"
        };

        // Act
        var response =
            await client.PostAsJsonAsync("/api/Cities", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result =
            await response.Content.ReadFromJsonAsync<CityResponseDto>();

        Assert.NotNull(result);
        Assert.True(result.CityId > 0);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.Country, result.Country);
        Assert.Equal(request.PostOffice, result.PostOffice);

        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();

        var city = await dbContext.Cities
            .SingleOrDefaultAsync(c => c.CityId == result.CityId);

        Assert.NotNull(city);
        Assert.Equal(request.Name, city.Name);
        Assert.Equal(request.Country, city.Country);
        Assert.Equal(request.PostOffice, city.PostOffice);
    }

    [Fact]
    public async Task CreateCity_WhenNameIsMissing_ShouldReturnBadRequest()
    {
        // Arrange
        var admin = await CreateUserAsync(
            "admin_invalid_city",
            "admin_invalid_city@test.com",
            Role.Admin);

        using var client = CreateAuthenticatedClient(admin);

        var request = new CityRequestDto
        {
            Name = "",
            Country = "Palestine",
            PostOffice = "P101"
        };

        // Act
        var response =
            await client.PostAsJsonAsync("/api/Cities", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCity_WhenCityExists_ShouldReturnOkAndUpdateDatabase()
    {
        // Arrange
        var city = await CreateCityAsync(
            "Old City",
            "Old Country",
            "OLD");

        var admin = await CreateUserAsync(
            "admin_update_city",
            "admin_update_city@test.com",
            Role.Admin);

        using var client = CreateAuthenticatedClient(admin);

        var request = new CityRequestDto
        {
            Name = "Updated City",
            Country = "Updated Country",
            PostOffice = "NEW"
        };

        // Act
        var response =
            await client.PutAsJsonAsync(
                $"/api/Cities/{city.CityId}",
                request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result =
            await response.Content.ReadFromJsonAsync<CityResponseDto>();

        Assert.NotNull(result);
        Assert.Equal(city.CityId, result.CityId);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.Country, result.Country);
        Assert.Equal(request.PostOffice, result.PostOffice);

        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();

        var updatedCity =
            await dbContext.Cities.FindAsync(city.CityId);

        Assert.NotNull(updatedCity);
        Assert.Equal(request.Name, updatedCity.Name);
        Assert.Equal(request.Country, updatedCity.Country);
        Assert.Equal(request.PostOffice, updatedCity.PostOffice);
        Assert.NotNull(updatedCity.UpdatedAt);
    }

    [Fact]
    public async Task UpdateCity_WhenCityDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var admin = await CreateUserAsync(
            "admin_update_missing",
            "admin_update_missing@test.com",
            Role.Admin);

        using var client = CreateAuthenticatedClient(admin);

        var request = new CityRequestDto
        {
            Name = "City",
            Country = "Country",
            PostOffice = "POST"
        };

        // Act
        var response =
            await client.PutAsJsonAsync(
                "/api/Cities/999999",
                request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCity_WhenCityExistsAndHasNoHotels_ShouldReturnNoContentAndDeleteCity()
    {
        // Arrange
        var city = await CreateCityAsync(
            "Delete City",
            "Delete Country",
            "DEL");

        var admin = await CreateUserAsync(
            "admin_delete_city",
            "admin_delete_city@test.com",
            Role.Admin);

        using var client = CreateAuthenticatedClient(admin);

        // Act
        var response =
            await client.DeleteAsync(
                $"/api/Cities/{city.CityId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();

        var deletedCity =
            await dbContext.Cities.FindAsync(city.CityId);

        Assert.Null(deletedCity);
    }

    [Fact]
    public async Task DeleteCity_WhenCityDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var admin = await CreateUserAsync(
            "admin_delete_missing",
            "admin_delete_missing@test.com",
            Role.Admin);

        using var client = CreateAuthenticatedClient(admin);

        // Act
        var response =
            await client.DeleteAsync("/api/Cities/999998");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCity_WhenCityHasHotels_ShouldReturnConflictAndKeepCity()
    {
        // Arrange
        var city = await CreateCityAsync(
            "City With Hotel",
            "Palestine",
            "HOTEL");

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext =
                scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();

            var hotel = new Hotel(
                "Test Hotel",
                "Test Owner",
                "Test Address",
                32.2211,
                35.2544,
                HotelType.Luxury,
                city.CityId,
                "Test Description",
                "Test History");

            dbContext.Hotels.Add(hotel);
            await dbContext.SaveChangesAsync();
        }

        var admin = await CreateUserAsync(
            "admin_delete_city_with_hotel",
            "admin_delete_city_with_hotel@test.com",
            Role.Admin);

        using var client = CreateAuthenticatedClient(admin);

        // Act
        var response =
            await client.DeleteAsync(
                $"/api/Cities/{city.CityId}");

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var verificationScope =
            _factory.Services.CreateScope();

        var verificationContext =
            verificationScope.ServiceProvider
                .GetRequiredService<HotelBookingDbContext>();

        var cityStillExists =
            await verificationContext.Cities
                .AnyAsync(c => c.CityId == city.CityId);

        Assert.True(cityStillExists);
    }

    [Fact]
    public async Task GetCities_WhenSearchMatches_ShouldReturnMatchingCities()
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N");

        await CreateCityAsync(
            $"SearchTarget-{unique}",
            "Palestine",
            "S1");

        await CreateCityAsync(
            $"OtherCity-{unique}",
            "Jordan",
            "S2");

        var admin = await CreateUserAsync(
            $"admin_search_{unique}",
            $"{unique}@test.com",
            Role.Admin);

        using var client = CreateAuthenticatedClient(admin);

        // Act
        var response =
            await client.GetAsync(
                $"/api/Cities?search=SearchTarget-{unique}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var cities =
            await response.Content
                .ReadFromJsonAsync<List<CityResponseDto>>();

        Assert.NotNull(cities);

        Assert.Contains(
            cities,
            c => c.Name == $"SearchTarget-{unique}");

        Assert.DoesNotContain(
            cities,
            c => c.Name == $"OtherCity-{unique}");
    }

    private async Task<User> CreateUserAsync(
        string username,
        string email,
        Role role)
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();

        var passwordHasher =
            scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var user = new User(
            "Test",
            "User",
            username,
            email,
            passwordHasher.HashPassword("Password123"),
            role);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        return user;
    }

    private async Task<City> CreateCityAsync(
        string name,
        string country,
        string postOffice)
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();

        var city = new City(
            name,
            country,
            postOffice);

        dbContext.Cities.Add(city);
        await dbContext.SaveChangesAsync();

        return city;
    }

    private HttpClient CreateAuthenticatedClient(User user)
    {
        using var scope = _factory.Services.CreateScope();

        var jwtTokenGenerator =
            scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();

        var token = jwtTokenGenerator.GenerateToken(user);

        var client = _factory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);

        return client;
    }
}