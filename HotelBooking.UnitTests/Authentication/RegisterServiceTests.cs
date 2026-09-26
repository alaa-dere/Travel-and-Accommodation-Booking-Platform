using HotelBooking.Application.Authentication.Register;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using Moq;

namespace HotelBooking.UnitTests.Authentication.Register;

public class RegisterServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly RegisterService _service;

    public RegisterServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _service = new RegisterService(_userRepositoryMock.Object, _passwordHasherMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_WhenUsernameAlreadyExists_ShouldThrowConflictException()
    {
        // Arrange
        var request = CreateValidRequest();
        _userRepositoryMock.Setup(repository => repository.UsernameExistsAsync(request.Username)).ReturnsAsync(true);

        // Act
        var action = async () => await _service.RegisterAsync(request);

        // Assert
        await Assert.ThrowsAsync<ConflictException>(action);

        _userRepositoryMock.Verify(repository => repository.EmailExistsAsync(It.IsAny<string>()), Times.Never);
        _passwordHasherMock.Verify(hasher => hasher.HashPassword(It.IsAny<string>()), Times.Never);
        _userRepositoryMock.Verify(repository => repository.Add(It.IsAny<User>()), Times.Never);
        _userRepositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailAlreadyExists_ShouldThrowConflictException()
    {
        // Arrange
        var request = CreateValidRequest();
        _userRepositoryMock.Setup(repository => repository.UsernameExistsAsync(request.Username)).ReturnsAsync(false);
        _userRepositoryMock.Setup(repository => repository.EmailExistsAsync(request.Email)).ReturnsAsync(true);

        // Act
        var action = async () => await _service.RegisterAsync(request);

        // Assert
        await Assert.ThrowsAsync<ConflictException>(action);

        _passwordHasherMock.Verify(hasher => hasher.HashPassword(It.IsAny<string>()), Times.Never);
        _userRepositoryMock.Verify(repository => repository.Add(It.IsAny<User>()), Times.Never);
        _userRepositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WhenRequestIsValid_ShouldCreateAndSaveCustomer()
    {
        // Arrange
        var request = CreateValidRequest();
        const string hashedPassword = "hashed-password";

        _userRepositoryMock.Setup(repository => repository.UsernameExistsAsync(request.Username)).ReturnsAsync(false);
        _userRepositoryMock.Setup(repository => repository.EmailExistsAsync(request.Email)) .ReturnsAsync(false);
        _passwordHasherMock.Setup(hasher => hasher.HashPassword(request.Password)).Returns(hashedPassword);

        // Act
        await _service.RegisterAsync(request);

        // Assert
        _passwordHasherMock.Verify(hasher => hasher.HashPassword(request.Password), Times.Once);
        _userRepositoryMock.Verify(
            repository => repository.Add(
                It.Is<User>(user =>
                    user.FirstName == request.FirstName &&
                    user.LastName == request.LastName &&
                    user.Username == request.Username &&
                    user.Email == request.Email &&
                    user.PasswordHash == hashedPassword &&
                    user.Role == Role.Customer)),
            Times.Once);

        _userRepositoryMock.Verify(repository => repository.SaveChangesAsync(), Times.Once);
    }

    private static RegisterRequestDto CreateValidRequest()
    {
        return new RegisterRequestDto
        {
            FirstName = "Alaa",
            LastName = "Test",
            Username = "alaa",
            Email = "alaa@test.com",
            Password = "Password123!"
        };
    }
}