using System.IdentityModel.Tokens.Jwt;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using System.Security.Claims;
using System.Text;
using HotelBooking.Application.Common.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HotelBooking.Infrastructure.Authentication;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtSettings _jwtSettings;
    private readonly TimeProvider _timeProvider;

    public JwtTokenGenerator(IOptions<JwtSettings> jwtOptions, TimeProvider timeProvider)
    {
        _jwtSettings = jwtOptions.Value;
        _timeProvider = timeProvider;
    }
    public string GenerateToken(User user)
    {
        var claims = new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
        };
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var credentials  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var issuer = _jwtSettings.Issuer;
        var audience = _jwtSettings.Audience;
        var issuedAt = _timeProvider.GetUtcNow().UtcDateTime;
        var expires = issuedAt.AddMinutes(_jwtSettings.ExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: issuedAt,
            expires: expires,
            signingCredentials: credentials
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
