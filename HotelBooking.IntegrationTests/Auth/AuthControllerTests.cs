using System.Net;
using System.Net.Http.Json;
using HotelBooking.Application.Authentication.Login;
using HotelBooking.Application.Authentication.Register;
using HotelBooking.Domain.Entities;
using HotelBooking.IntegrationTests.Infrastructure;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.IntegrationTests.Auth;

public class AuthControllerTests :
    IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WhenRequestIsValid_ShouldReturnOkAndSaveUser()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            FirstName = "Alaa",
            LastName = "Test",
            Username = "register_valid_user",
            Email = "register_valid@test.com",
            Password = "Password123"
        };

        // Act
        var response =
            await _client.PostAsJsonAsync("/api/Auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();

        var user = await dbContext.Users
            .SingleOrDefaultAsync(u => u.Username == request.Username);

        Assert.NotNull(user);

        Assert.Equal(request.FirstName, user.FirstName);
        Assert.Equal(request.LastName, user.LastName);
        Assert.Equal(request.Username, user.Username);
        Assert.Equal(request.Email, user.Email);
        Assert.Equal(Role.Customer, user.Role);

        Assert.NotEqual(request.Password, user.PasswordHash);
        Assert.False(string.IsNullOrWhiteSpace(user.PasswordHash));
    }

    [Fact]
    public async Task Register_WhenUsernameAlreadyExists_ShouldReturnConflict()
    {
        // Arrange
        var firstRequest = new RegisterRequestDto
        {
            FirstName = "First",
            LastName = "User",
            Username = "duplicate_username",
            Email = "first@test.com",
            Password = "Password123"
        };

        var secondRequest = new RegisterRequestDto
        {
            FirstName = "Second",
            LastName = "User",
            Username = "duplicate_username",
            Email = "second@test.com",
            Password = "Password456"
        };

        await _client.PostAsJsonAsync(
            "/api/Auth/register",
            firstRequest);

        // Act
        var response =
            await _client.PostAsJsonAsync(
                "/api/Auth/register",
                secondRequest);

        // Assert
        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task Register_WhenEmailAlreadyExists_ShouldReturnConflict()
    {
        // Arrange
        var firstRequest = new RegisterRequestDto
        {
            FirstName = "First",
            LastName = "User",
            Username = "email_user_one",
            Email = "duplicate@test.com",
            Password = "Password123"
        };

        var secondRequest = new RegisterRequestDto
        {
            FirstName = "Second",
            LastName = "User",
            Username = "email_user_two",
            Email = "duplicate@test.com",
            Password = "Password456"
        };

        await _client.PostAsJsonAsync(
            "/api/Auth/register",
            firstRequest);

        // Act
        var response =
            await _client.PostAsJsonAsync(
                "/api/Auth/register",
                secondRequest);

        // Assert
        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task Register_WhenEmailIsInvalid_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            FirstName = "Alaa",
            LastName = "Test",
            Username = "invalid_email_user",
            Email = "not-an-email",
            Password = "Password123"
        };

        // Act
        var response =
            await _client.PostAsJsonAsync(
                "/api/Auth/register",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Register_WhenPasswordIsTooShort_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            FirstName = "Alaa",
            LastName = "Test",
            Username = "short_password_user",
            Email = "shortpassword@test.com",
            Password = "1234567"
        };

        // Act
        var response =
            await _client.PostAsJsonAsync(
                "/api/Auth/register",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Register_WhenRequiredFieldIsMissing_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            FirstName = "",
            LastName = "Test",
            Username = "missing_field_user",
            Email = "missing@test.com",
            Password = "Password123"
        };

        // Act
        var response =
            await _client.PostAsJsonAsync(
                "/api/Auth/register",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Login_WhenCredentialsAreValid_ShouldReturnOkWithToken()
    {
        // Arrange
        var registerRequest = new RegisterRequestDto
        {
            FirstName = "Login",
            LastName = "User",
            Username = "valid_login_user",
            Email = "validlogin@test.com",
            Password = "Password123"
        };

        await _client.PostAsJsonAsync(
            "/api/Auth/register",
            registerRequest);

        var loginRequest = new LoginRequestDto
        {
            Username = registerRequest.Username,
            Password = registerRequest.Password
        };

        // Act
        var response =
            await _client.PostAsJsonAsync(
                "/api/Auth/login",
                loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result =
            await response.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
    }

    [Fact]
    public async Task Login_WhenUsernameDoesNotExist_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "user_that_does_not_exist",
            Password = "Password123"
        };

        // Act
        var response =
            await _client.PostAsJsonAsync(
                "/api/Auth/login",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Login_WhenPasswordIsIncorrect_ShouldReturnUnauthorized()
    {
        // Arrange
        var registerRequest = new RegisterRequestDto
        {
            FirstName = "Wrong",
            LastName = "Password",
            Username = "wrong_password_user",
            Email = "wrongpassword@test.com",
            Password = "Password123"
        };

        await _client.PostAsJsonAsync(
            "/api/Auth/register",
            registerRequest);

        var loginRequest = new LoginRequestDto
        {
            Username = registerRequest.Username,
            Password = "WrongPassword123"
        };

        // Act
        var response =
            await _client.PostAsJsonAsync(
                "/api/Auth/login",
                loginRequest);

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Login_WhenUsernameIsMissing_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "",
            Password = "Password123"
        };

        // Act
        var response =
            await _client.PostAsJsonAsync(
                "/api/Auth/login",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Login_WhenPasswordIsTooShort_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "valid_username",
            Password = "1234567"
        };

        // Act
        var response =
            await _client.PostAsJsonAsync(
                "/api/Auth/login",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    private sealed class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
    }
}