using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Exceptions;

namespace HotelBooking.Application.Authentication.Login;

public class LoginService : ILoginService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public LoginService(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task LoginAsync(LoginRequestDto request)
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
    }
}