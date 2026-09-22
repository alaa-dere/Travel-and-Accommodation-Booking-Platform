using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Exceptions;

namespace HotelBooking.Application.Authentication.Login;

public class LoginService : ILoginService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginService(IUserRepository userRepository, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<string> LoginAsync(LoginRequestDto request)
    {
        var username = request.Username;
        var user = await _userRepository.GetByUsernameAsync(username);
        if (user == null)
        {
            throw new UnauthorizedException("Invalid username or password");
        }
        
        var password = request.Password;
        bool correctPassword = _passwordHasher.VerifyPassword(user.PasswordHash, password);
        if (!correctPassword)
        {
            throw new UnauthorizedException("Invalid username or password");
        }
        
        var token = _jwtTokenGenerator.GenerateToken(user);
        return token;
    }
}