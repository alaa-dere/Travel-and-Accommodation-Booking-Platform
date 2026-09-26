using HotelBooking.Application.Authentication.Login;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using Moq;

namespace HotelBooking.UnitTests.Authentication.Login;

public class LoginServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock;
    private readonly LoginService _service;

    public LoginServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _jwtTokenGeneratorMock = new Mock<IJwtTokenGenerator>();

        _service = new LoginService(_userRepositoryMock.Object, _passwordHasherMock.Object, _jwtTokenGeneratorMock.Object);
    }

    [Fact]
    public async Task LoginAsync_WhenUserDoesNotExist_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "alaa",
            Password = "password123"
        };

        _userRepositoryMock.Setup(repository => repository.GetByUsernameAsync(request.Username)).ReturnsAsync((User?)null);

        // Act
        var action = async () => await _service.LoginAsync(request);

        // Assert
        await Assert.ThrowsAsync<UnauthorizedException>(action);

        _passwordHasherMock.Verify(hasher => hasher.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _jwtTokenGeneratorMock.Verify(generator => generator.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordIsIncorrect_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var user = CreateUser();
        var request = new LoginRequestDto
        {
            Username = "alaa",
            Password = "wrongPassword"
        };

        _userRepositoryMock.Setup(repository => repository.GetByUsernameAsync(request.Username)).ReturnsAsync(user);
        _passwordHasherMock.Setup(hasher => hasher.VerifyPassword(user.PasswordHash, request.Password)).Returns(false);

        // Act
        var action = async () => await _service.LoginAsync(request);

        // Assert
        await Assert.ThrowsAsync<UnauthorizedException>(action);

        _jwtTokenGeneratorMock.Verify(generator => generator.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenCredentialsAreValid_ShouldReturnGeneratedToken()
    {
        // Arrange
        var user = CreateUser();

        var request = new LoginRequestDto
        {
            Username = "alaa",
            Password = "password123"
        };

        const string expectedToken = "generated-jwt-token";

        _userRepositoryMock.Setup(repository => repository.GetByUsernameAsync(request.Username)).ReturnsAsync(user);
        _passwordHasherMock.Setup(hasher => hasher.VerifyPassword(user.PasswordHash, request.Password)).Returns(true);
        _jwtTokenGeneratorMock.Setup(generator => generator.GenerateToken(user)).Returns(expectedToken);

        // Act
        var result = await _service.LoginAsync(request);

        // Assert
        Assert.Equal(expectedToken, result);

        _passwordHasherMock.Verify(hasher => hasher.VerifyPassword(user.PasswordHash, request.Password), Times.Once);
        _jwtTokenGeneratorMock.Verify(generator => generator.GenerateToken(user), Times.Once);
    }

    private static User CreateUser()
    {
        return new User(
            firstName: "Alaa",
            lastName: "Test",
            username: "alaa",
            email: "alaa@test.com",
            passwordHash: "hashed-password",
            role: Role.Customer);
    }
}