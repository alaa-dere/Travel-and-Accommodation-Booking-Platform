using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Authentication.Register;

public class RegisterService : IRegisterService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterService(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task RegisterAsync(RegisterRequestDto request)
    {
        var username = request.Username;
        bool usernameExists = await _userRepository.UsernameExistsAsync(username);
        if (usernameExists)
        {
            throw new ConflictException($"Username {username} already exists");
        }
        
        var email = request.Email;
        bool emailExists = await _userRepository.EmailExistsAsync(email);
        if (emailExists)
        {
            throw new ConflictException($"Email {email} already exists");
        }
        
        var password = request.Password;
        var passwordHash = _passwordHasher.HashPassword(password);

        var user = new User(request.FirstName, request.LastName, username, email, passwordHash);
        
        _userRepository.Add(user);
        await _userRepository.SaveChangesAsync();
    }
}