using HotelBooking.Domain.Entities;

namespace HotelBooking.UnitTests.Domain.Entities;

public class InvoiceTests
{
    [Fact]
    public void Constructor_WhenDataIsValid_ShouldCreateInvoiceWithCorrectValues()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var invoice = new Invoice(
            userId: 1,
            hotelId: 10,
            totalAmount: 500m);

        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(1, invoice.UserId);
        Assert.Equal(10, invoice.HotelId);
        Assert.Equal(500m, invoice.TotalAmount);

        Assert.InRange(
            invoice.CreatedAt,
            beforeCreation,
            afterCreation);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenUserIdIsInvalid_ShouldThrowArgumentOutOfRangeException(
        int userId)
    {
        // Act
        var action = () =>
            new Invoice(
                userId,
                hotelId: 10,
                totalAmount: 500m);

        // Assert
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(action);

        Assert.Equal("userId", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenHotelIdIsInvalid_ShouldThrowArgumentOutOfRangeException(
        int hotelId)
    {
        // Act
        var action = () =>
            new Invoice(
                userId: 1,
                hotelId,
                totalAmount: 500m);

        // Assert
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(action);

        Assert.Equal("hotelId", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenTotalAmountIsInvalid_ShouldThrowArgumentOutOfRangeException(
        int totalAmount)
    {
        // Act
        var action = () =>
            new Invoice(
                userId: 1,
                hotelId: 10,
                totalAmount);

        // Assert
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(action);

        Assert.Equal("totalAmount", exception.ParamName);
    }

    [Fact]
    public void Constructor_WhenTotalAmountIsPositive_ShouldSucceed()
    {
        // Act
        var invoice = new Invoice(
            userId: 1,
            hotelId: 10,
            totalAmount: 0.01m);

        // Assert
        Assert.Equal(0.01m, invoice.TotalAmount);
    }

    [Fact]
    public void Constructor_ShouldInitializeBookingsAsEmptyCollection()
    {
        // Act
        var invoice = CreateValidInvoice();

        // Assert
        Assert.NotNull(invoice.Bookings);
        Assert.Empty(invoice.Bookings);
    }

    [Fact]
    public void UpdateTotal_WhenTotalAmountIsValid_ShouldUpdateTotalAmount()
    {
        // Arrange
        var invoice = CreateValidInvoice();

        // Act
        invoice.UpdateTotal(750m);

        // Assert
        Assert.Equal(750m, invoice.TotalAmount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void UpdateTotal_WhenTotalAmountIsInvalid_ShouldThrowArgumentException(
        int totalAmount)
    {
        // Arrange
        var invoice = CreateValidInvoice();

        // Act
        var action = () =>
            invoice.UpdateTotal(totalAmount);

        // Assert
        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("totalAmount", exception.ParamName);
    }

    [Fact]
    public void UpdateTotal_WhenTotalAmountIsInvalid_ShouldNotChangeExistingTotal()
    {
        // Arrange
        var invoice = CreateValidInvoice();

        var originalTotal = invoice.TotalAmount;

        // Act
        try
        {
            invoice.UpdateTotal(0);
        }
        catch (ArgumentException)
        {
        }

        // Assert
        Assert.Equal(
            originalTotal,
            invoice.TotalAmount);
    }

    [Fact]
    public void UpdateTotal_WhenTotalAmountIsSmallPositiveValue_ShouldSucceed()
    {
        // Arrange
        var invoice = CreateValidInvoice();

        // Act
        invoice.UpdateTotal(0.01m);

        // Assert
        Assert.Equal(0.01m, invoice.TotalAmount);
    }

    private static Invoice CreateValidInvoice()
    {
        return new Invoice(
            userId: 1,
            hotelId: 10,
            totalAmount: 500m);
    }
}