using HotelBooking.Application.Emails;
using HotelBooking.Application.Emails.Dtos;
using HotelBooking.Domain.Enums;
using Moq;

namespace HotelBooking.UnitTests.Emails;

public class BookingConfirmationEmailServiceTests
{
    private readonly Mock<IEmailSender> _emailSenderMock;
    private readonly BookingConfirmationEmailService _service;

    public BookingConfirmationEmailServiceTests()
    {
        _emailSenderMock = new Mock<IEmailSender>();

        _service = new BookingConfirmationEmailService(
            _emailSenderMock.Object);
    }

    [Fact]
    public async Task SendAsync_ShouldSendEmailToCorrectCustomer()
    {
        // Arrange
        var confirmation = CreateConfirmation();

        // Act
        await _service.SendAsync(confirmation);

        // Assert
        _emailSenderMock.Verify(
            sender => sender.SendAsync(
                confirmation.CustomerEmail,
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_ShouldCreateCorrectSubject()
    {
        // Arrange
        var confirmation = CreateConfirmation();

        // Act
        await _service.SendAsync(confirmation);

        // Assert
        _emailSenderMock.Verify(
            sender => sender.SendAsync(
                It.IsAny<string>(),
                $"Booking Confirmation - Invoice #{confirmation.InvoiceId}",
                It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_ShouldIncludeHotelInformationInBody()
    {
        // Arrange
        var confirmation = CreateConfirmation();

        string? capturedBody = null;

        _emailSenderMock
            .Setup(sender => sender.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Callback<string, string, string>(
                (_, _, body) => capturedBody = body);

        // Act
        await _service.SendAsync(confirmation);

        // Assert
        Assert.NotNull(capturedBody);
        Assert.Contains("Your booking has been confirmed.", capturedBody);
        Assert.Contains("BOOKING CONFIRMATION", capturedBody);
        Assert.Contains($"Hotel: {confirmation.HotelName}", capturedBody);
    }

    [Fact]
    public async Task SendAsync_ShouldIncludeRoomInformationInBody()
    {
        // Arrange
        var confirmation = CreateConfirmation();
        var room = confirmation.Rooms[0];

        string? capturedBody = null;

        _emailSenderMock
            .Setup(sender => sender.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Callback<string, string, string>(
                (_, _, body) => capturedBody = body);

        // Act
        await _service.SendAsync(confirmation);

        // Assert
        Assert.NotNull(capturedBody);
        Assert.Contains($"Booking #{room.BookingId}", capturedBody);
        Assert.Contains($"Room: {room.RoomNumber}", capturedBody);
        Assert.Contains($"Check-in: {room.CheckIn:yyyy-MM-dd}", capturedBody);
        Assert.Contains($"Check-out: {room.CheckOut:yyyy-MM-dd}", capturedBody);
        Assert.Contains($"Booking Total: {room.TotalPrice:0.00}", capturedBody);
    }

    [Fact]
    public async Task SendAsync_WhenMultipleRoomsExist_ShouldIncludeAllRoomsInBody()
    {
        // Arrange
        var confirmation = CreateConfirmation();

        confirmation.Rooms.Add(
            new BookingConfirmationEmailRoomDto
            {
                BookingId = 102,
                RoomNumber = "202",
                CheckIn = new DateTime(2026, 10, 10),
                CheckOut = new DateTime(2026, 10, 13),
                TotalPrice = 450m
            });

        string? capturedBody = null;

        _emailSenderMock
            .Setup(sender => sender.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Callback<string, string, string>(
                (_, _, body) => capturedBody = body);

        // Act
        await _service.SendAsync(confirmation);

        // Assert
        Assert.NotNull(capturedBody);

        foreach (var room in confirmation.Rooms)
        {
            Assert.Contains($"Booking #{room.BookingId}", capturedBody);
            Assert.Contains($"Room: {room.RoomNumber}", capturedBody);
            Assert.Contains($"Check-in: {room.CheckIn:yyyy-MM-dd}", capturedBody);
            Assert.Contains($"Check-out: {room.CheckOut:yyyy-MM-dd}", capturedBody);
            Assert.Contains($"Booking Total: {room.TotalPrice:0.00}", capturedBody);
        }
    }

    [Fact]
    public async Task SendAsync_ShouldIncludeInvoiceInformationInBody()
    {
        // Arrange
        var confirmation = CreateConfirmation();

        string? capturedBody = null;

        _emailSenderMock
            .Setup(sender => sender.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Callback<string, string, string>(
                (_, _, body) => capturedBody = body);

        // Act
        await _service.SendAsync(confirmation);

        // Assert
        Assert.NotNull(capturedBody);
        Assert.Contains("INVOICE INFORMATION", capturedBody);
        Assert.Contains($"Invoice #{confirmation.InvoiceId}", capturedBody);
        Assert.Contains($"Invoice Total: {confirmation.InvoiceTotal:0.00}", capturedBody);
        Assert.Contains($"Payment Status: {confirmation.PaymentStatus}", capturedBody);
    }

    [Fact]
    public async Task SendAsync_ShouldIncludeInvoiceDownloadMessageInBody()
    {
        // Arrange
        var confirmation = CreateConfirmation();

        string? capturedBody = null;

        _emailSenderMock
            .Setup(sender => sender.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Callback<string, string, string>(
                (_, _, body) => capturedBody = body);

        // Act
        await _service.SendAsync(confirmation);

        // Assert
        Assert.NotNull(capturedBody);

        Assert.Contains(
            "You can download the full invoice PDF from your account.",
            capturedBody);
    }

    [Fact]
    public async Task SendAsync_WhenNoRoomsExist_ShouldStillSendEmail()
    {
        // Arrange
        var confirmation = CreateConfirmation();
        confirmation.Rooms.Clear();

        // Act
        await _service.SendAsync(confirmation);

        // Assert
        _emailSenderMock.Verify(
            sender => sender.SendAsync(
                confirmation.CustomerEmail,
                $"Booking Confirmation - Invoice #{confirmation.InvoiceId}",
                It.Is<string>(body =>
                    body.Contains("BOOKING CONFIRMATION") &&
                    body.Contains("INVOICE INFORMATION"))),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_WhenEmailSenderThrows_ShouldPropagateException()
    {
        // Arrange
        var confirmation = CreateConfirmation();

        _emailSenderMock
            .Setup(sender => sender.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Email sending failed."));

        // Act
        var action = async () => await _service.SendAsync(confirmation);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(action);

        _emailSenderMock.Verify(
            sender => sender.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Once);
    }

    private static BookingConfirmationEmailDto CreateConfirmation()
    {
        return new BookingConfirmationEmailDto
        {
            CustomerEmail = "customer@example.com",
            InvoiceId = 50,
            HotelName = "Test Hotel",
            InvoiceTotal = 300m,
            PaymentStatus = PaymentStatus.Paid,

            Rooms = new List<BookingConfirmationEmailRoomDto>
            {
                new()
                {
                    BookingId = 101,
                    RoomNumber = "101",
                    CheckIn = new DateTime(2026, 10, 10),
                    CheckOut = new DateTime(2026, 10, 13),
                    TotalPrice = 300m
                }
            }
        };
    }
}