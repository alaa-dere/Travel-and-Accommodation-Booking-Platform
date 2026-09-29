using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HotelBooking.Application.Common.Settings;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Authentication;
using Microsoft.Extensions.Options;

namespace HotelBooking.IntegrationTests.Auth;

public class JwtTokenGeneratorTests
{
    [Fact]
    public void GenerateToken_ShouldUseConfiguredIdentityClaimsAndLifetime()
    {
        // Arrange
        var issuedAt = new DateTimeOffset(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var settings = new JwtSettings
        {
            Key = "12345678901234567890123456789012",
            Issuer = "HotelBooking.Tests",
            Audience = "HotelBooking.Tests.Clients",
            ExpirationMinutes = 60
        };
        var generator = new JwtTokenGenerator(
            Options.Create(settings),
            new FixedTimeProvider(issuedAt));
        var user = new User(
            "Alaa",
            "Test",
            "alaa",
            "alaa@test.com",
            "hashed-password",
            Role.Customer)
        {
            UserId = 42
        };

        // Act
        var encodedToken = generator.GenerateToken(user);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(encodedToken);

        // Assert
        Assert.Equal(settings.Issuer, token.Issuer);
        Assert.Contains(settings.Audience, token.Audiences);
        Assert.Equal("42", token.Claims.Single(claim => claim.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("alaa", token.Claims.Single(claim => claim.Type == ClaimTypes.Name).Value);
        Assert.Equal(Role.Customer.ToString(), token.Claims.Single(claim => claim.Type == ClaimTypes.Role).Value);
        Assert.Equal(issuedAt.UtcDateTime, token.ValidFrom);
        Assert.Equal(issuedAt.AddMinutes(60).UtcDateTime, token.ValidTo);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
