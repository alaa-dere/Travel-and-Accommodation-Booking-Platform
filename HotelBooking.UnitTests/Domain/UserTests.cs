using HotelBooking.Domain.Entities;

namespace HotelBooking.UnitTests.Domain.Entities;

public class UserTests
{
    [Fact]
    public void Constructor_WhenDataIsValid_ShouldCreateUserWithCorrectValues()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var user = CreateValidUser();

        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal("Alaa", user.FirstName);
        Assert.Equal("Test", user.LastName);
        Assert.Equal("alaa", user.Username);
        Assert.Equal("alaa@example.com", user.Email);
        Assert.Equal("hashed-password", user.PasswordHash);
        Assert.Equal(Role.Customer, user.Role);

        Assert.InRange(
            user.CreatedAt,
            beforeCreation,
            afterCreation);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenFirstNameIsInvalid_ShouldThrowArgumentException(
        string firstName)
    {
        // Act
        var action = () =>
            CreateValidUser(firstName: firstName);

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("firstName", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenLastNameIsInvalid_ShouldThrowArgumentException(
        string lastName)
    {
        // Act
        var action = () =>
            CreateValidUser(lastName: lastName);

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("lastName", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenUsernameIsInvalid_ShouldThrowArgumentException(
        string username)
    {
        // Act
        var action = () =>
            CreateValidUser(username: username);

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("username", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenEmailIsInvalid_ShouldThrowArgumentException(
        string email)
    {
        // Act
        var action = () =>
            CreateValidUser(email: email);

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("email", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenPasswordHashIsInvalid_ShouldThrowArgumentException(
        string passwordHash)
    {
        // Act
        var action = () =>
            CreateValidUser(passwordHash: passwordHash);

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("passwordHash", exception.ParamName);
    }

    [Theory]
    [InlineData(Role.Customer)]
    [InlineData(Role.Admin)]
    public void Constructor_ShouldSetProvidedRole(Role role)
    {
        // Act
        var user = CreateValidUser(role: role);

        // Assert
        Assert.Equal(role, user.Role);
    }

    [Fact]
    public void Constructor_ShouldInitializeBookingsAsEmptyCollection()
    {
        // Act
        var user = CreateValidUser();

        // Assert
        Assert.NotNull(user.Bookings);
        Assert.Empty(user.Bookings);
    }

    [Fact]
    public void Constructor_ShouldInitializeRecentlyVisitedHotelsAsEmptyCollection()
    {
        // Act
        var user = CreateValidUser();

        // Assert
        Assert.NotNull(user.RecentlyVisitedHotels);
        Assert.Empty(user.RecentlyVisitedHotels);
    }

    [Fact]
    public void Constructor_ShouldInitializeCartItemsAsEmptyCollection()
    {
        // Act
        var user = CreateValidUser();

        // Assert
        Assert.NotNull(user.CartItems);
        Assert.Empty(user.CartItems);
    }

    [Fact]
    public void Constructor_ShouldInitializeInvoicesAsEmptyCollection()
    {
        // Act
        var user = CreateValidUser();

        // Assert
        Assert.NotNull(user.Invoices);
        Assert.Empty(user.Invoices);
    }

    private static User CreateValidUser(
        string firstName = "Alaa",
        string lastName = "Test",
        string username = "alaa",
        string email = "alaa@example.com",
        string passwordHash = "hashed-password",
        Role role = Role.Customer)
    {
        return new User(
            firstName,
            lastName,
            username,
            email,
            passwordHash,
            role);
    }
}