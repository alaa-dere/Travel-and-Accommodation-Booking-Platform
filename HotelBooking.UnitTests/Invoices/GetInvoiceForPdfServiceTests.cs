using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Invoices;
using HotelBooking.Domain.Entities;
using Moq;

namespace HotelBooking.UnitTests.Invoices;

public class GetInvoiceForPdfServiceTests
{
    private readonly Mock<IInvoiceRepository> _invoiceRepositoryMock;
    private readonly GetInvoiceForPdfService _service;

    public GetInvoiceForPdfServiceTests()
    {
        _invoiceRepositoryMock = new Mock<IInvoiceRepository>();
        _service = new GetInvoiceForPdfService(
            _invoiceRepositoryMock.Object);
    }

    [Fact]
    public async Task GetAsync_WhenInvoiceIdIsZero_ShouldThrowBadRequestException()
    {
        // Act
        var action = async () =>
            await _service.GetAsync(0, 1);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        _invoiceRepositoryMock.Verify(
            repository => repository.GetByIdForUserAsync(
                It.IsAny<int>(),
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetAsync_WhenInvoiceIdIsNegative_ShouldThrowBadRequestException()
    {
        // Act
        var action = async () =>
            await _service.GetAsync(-1, 1);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        _invoiceRepositoryMock.Verify(
            repository => repository.GetByIdForUserAsync(
                It.IsAny<int>(),
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetAsync_WhenUserIdIsZero_ShouldThrowBadRequestException()
    {
        // Act
        var action = async () =>
            await _service.GetAsync(1, 0);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        _invoiceRepositoryMock.Verify(
            repository => repository.GetByIdForUserAsync(
                It.IsAny<int>(),
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetAsync_WhenUserIdIsNegative_ShouldThrowBadRequestException()
    {
        // Act
        var action = async () =>
            await _service.GetAsync(1, -1);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        _invoiceRepositoryMock.Verify(
            repository => repository.GetByIdForUserAsync(
                It.IsAny<int>(),
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetAsync_WhenInvoiceDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        const int invoiceId = 10;
        const int userId = 5;

        _invoiceRepositoryMock
            .Setup(repository =>
                repository.GetByIdForUserAsync(
                    invoiceId,
                    userId))
            .ReturnsAsync((Invoice?)null);

        // Act
        var action = async () =>
            await _service.GetAsync(invoiceId, userId);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);
    }

    [Fact]
    public async Task GetAsync_ShouldRequestCorrectInvoiceForCorrectUser()
    {
        // Arrange
        const int invoiceId = 10;
        const int userId = 5;

        var invoice = CreateValidInvoice(
            invoiceId,
            userId);

        _invoiceRepositoryMock
            .Setup(repository =>
                repository.GetByIdForUserAsync(
                    invoiceId,
                    userId))
            .ReturnsAsync(invoice);

        // Act
        await _service.GetAsync(invoiceId, userId);

        // Assert
        _invoiceRepositoryMock.Verify(
            repository =>
                repository.GetByIdForUserAsync(
                    invoiceId,
                    userId),
            Times.Once);
    }

    [Fact]
    public async Task GetAsync_WhenPaymentIsMissing_ShouldThrowInvalidOperationException()
    {
        // Arrange
        const int invoiceId = 10;
        const int userId = 5;

        var invoice = CreateValidInvoice(
            invoiceId,
            userId);

        invoice.Payment = null;

        _invoiceRepositoryMock
            .Setup(repository =>
                repository.GetByIdForUserAsync(
                    invoiceId,
                    userId))
            .ReturnsAsync(invoice);

        // Act
        var action = async () =>
            await _service.GetAsync(invoiceId, userId);

        // Assert
        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                action);

        Assert.Equal(
            "Invoice payment information is missing.",
            exception.Message);
    }

    [Fact]
    public async Task GetAsync_WhenInvoiceExists_ShouldMapInvoiceFieldsCorrectly()
    {
        // Arrange
        const int invoiceId = 10;
        const int userId = 5;

        var invoice = CreateValidInvoice(
            invoiceId,
            userId);

        _invoiceRepositoryMock
            .Setup(repository =>
                repository.GetByIdForUserAsync(
                    invoiceId,
                    userId))
            .ReturnsAsync(invoice);

        // Act
        var result =
            await _service.GetAsync(invoiceId, userId);

        // Assert
        Assert.Equal(invoice.InvoiceId, result.InvoiceId);
        Assert.Equal(invoice.Hotel!.Name, result.HotelName);
        Assert.Equal(invoice.TotalAmount, result.TotalAmount);
        Assert.Equal(invoice.Payment!.Status, result.PaymentStatus);
        Assert.Equal(invoice.CreatedAt, result.CreatedAt);
    }

    [Fact]
    public async Task GetAsync_WhenHotelIsNull_ShouldReturnEmptyHotelName()
    {
        // Arrange
        const int invoiceId = 10;
        const int userId = 5;

        var invoice = CreateValidInvoice(
            invoiceId,
            userId);

        invoice.Hotel = null;

        _invoiceRepositoryMock
            .Setup(repository =>
                repository.GetByIdForUserAsync(
                    invoiceId,
                    userId))
            .ReturnsAsync(invoice);

        // Act
        var result =
            await _service.GetAsync(invoiceId, userId);

        // Assert
        Assert.Equal(string.Empty, result.HotelName);
    }

    [Fact]
    public async Task GetAsync_WhenInvoiceHasNoBookings_ShouldReturnEmptyBookings()
    {
        // Arrange
        const int invoiceId = 10;
        const int userId = 5;

        var invoice = CreateValidInvoice(
            invoiceId,
            userId);

        invoice.Bookings.Clear();

        _invoiceRepositoryMock
            .Setup(repository =>
                repository.GetByIdForUserAsync(
                    invoiceId,
                    userId))
            .ReturnsAsync(invoice);

        // Act
        var result =
            await _service.GetAsync(invoiceId, userId);

        // Assert
        Assert.Empty(result.Bookings);
    }

    [Fact]
    public async Task GetAsync_ShouldMapBookingFieldsCorrectly()
    {
        // Arrange
        const int invoiceId = 10;
        const int userId = 5;

        var invoice = CreateValidInvoice(
            invoiceId,
            userId);

        var booking = invoice.Bookings.Single();

        _invoiceRepositoryMock
            .Setup(repository =>
                repository.GetByIdForUserAsync(
                    invoiceId,
                    userId))
            .ReturnsAsync(invoice);

        // Act
        var result =
            await _service.GetAsync(invoiceId, userId);

        // Assert
        var bookingResult = Assert.Single(result.Bookings);

        Assert.Equal(
            booking.BookingId,
            bookingResult.BookingId);

        Assert.Equal(
            booking.Room!.RoomNumber,
            bookingResult.RoomNumber);

        Assert.Equal(
            booking.CheckIn,
            bookingResult.CheckIn);

        Assert.Equal(
            booking.CheckOut,
            bookingResult.CheckOut);

        Assert.Equal(
            booking.PricePerNight,
            bookingResult.PricePerNight);

        Assert.Equal(
            booking.OriginalTotalPrice,
            bookingResult.OriginalTotalPrice);

        Assert.Equal(
            booking.DiscountPercentage,
            bookingResult.DiscountPercentage);

        Assert.Equal(
            booking.DiscountAmount,
            bookingResult.DiscountAmount);

        Assert.Equal(
            booking.TotalPrice,
            bookingResult.TotalPrice);
    }

    [Fact]
    public async Task GetAsync_WhenBookingRoomIsNull_ShouldReturnEmptyRoomNumber()
    {
        // Arrange
        const int invoiceId = 10;
        const int userId = 5;

        var invoice = CreateValidInvoice(
            invoiceId,
            userId);

        invoice.Bookings.Single().Room = null;

        _invoiceRepositoryMock
            .Setup(repository =>
                repository.GetByIdForUserAsync(
                    invoiceId,
                    userId))
            .ReturnsAsync(invoice);

        // Act
        var result =
            await _service.GetAsync(invoiceId, userId);

        // Assert
        var booking = Assert.Single(result.Bookings);

        Assert.Equal(
            string.Empty,
            booking.RoomNumber);
    }

    [Fact]
    public async Task GetAsync_WhenInvoiceHasMultipleBookings_ShouldReturnAllBookings()
    {
        // Arrange
        const int invoiceId = 10;
        const int userId = 5;

        var invoice = CreateValidInvoice(
            invoiceId,
            userId);

        var secondBooking = CreateBooking(
            bookingId: 200,
            userId: userId,
            invoiceId: invoiceId,
            roomId: 30,
            roomNumber: "202");

        invoice.Bookings.Add(secondBooking);

        _invoiceRepositoryMock
            .Setup(repository =>
                repository.GetByIdForUserAsync(
                    invoiceId,
                    userId))
            .ReturnsAsync(invoice);

        // Act
        var result =
            await _service.GetAsync(invoiceId, userId);

        // Assert
        Assert.Equal(2, result.Bookings.Count);

        Assert.Contains(
            result.Bookings,
            booking =>
                booking.BookingId == 100 &&
                booking.RoomNumber == "101");

        Assert.Contains(
            result.Bookings,
            booking =>
                booking.BookingId == 200 &&
                booking.RoomNumber == "202");
    }

    private static Invoice CreateValidInvoice(
        int invoiceId,
        int userId)
    {
        var hotel = new Hotel(
            name: "Test Hotel",
            ownerName: "Test Owner",
            address: "Test Address",
            latitude: 32.2211,
            longitude: 35.2544,
            hotelType: (HotelType)1,
            cityId: 1,
            description: null,
            history: null)
        {
            HotelId = 20
        };

        var invoice = new Invoice(
            userId: userId,
            hotelId: hotel.HotelId,
            totalAmount: 300m)
        {
            InvoiceId = invoiceId,
            Hotel = hotel
        };

        var payment = new Payment(300m);
        payment.MarkAsPaid();

        invoice.Payment = payment;

        invoice.Bookings.Add(
            CreateBooking(
                bookingId: 100,
                userId: userId,
                invoiceId: invoiceId,
                roomId: 25,
                roomNumber: "101"));

        return invoice;
    }

    private static Booking CreateBooking(
        int bookingId,
        int userId,
        int invoiceId,
        int roomId,
        string roomNumber)
    {
        var room = new Room(
            roomNumber: roomNumber,
            roomType: (RoomType)1,
            pricePerNight: 100m,
            adultsCapacity: 2,
            childCapacity: 1,
            hotelId: 20,
            description: null)
        {
            RoomId = roomId
        };

        return new Booking(
            userId: userId,
            roomId: roomId,
            checkIn: new DateTime(2026, 10, 10),
            checkOut: new DateTime(2026, 10, 13),
            adults: 2,
            children: 0,
            pricePerNight: 100m,
            originalTotalPrice: 300m,
            discountPercentage: 0,
            discountAmount: 0m,
            totalPrice: 300m,
            specialRequests: null)
        {
            BookingId = bookingId,
            InvoiceId = invoiceId,
            Room = room
        };
    }
}