using HotelBooking.Application.Bookings;
using HotelBooking.Application.Bookings.Dtos;
using HotelBooking.Application.Checkout.Dtos;
using HotelBooking.Application.Emails;
using HotelBooking.Application.Emails.Dtos;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Payments;
using HotelBooking.Application.Payments.Dtos;
using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Enums;
using Moq;

namespace HotelBooking.UnitTests.Bookings;

public class CreateBookingsServiceTests
{
    private readonly Mock<ICartRepository> _cartRepositoryMock;
    private readonly Mock<IBookingRepository> _bookingRepositoryMock;
    private readonly Mock<IInvoiceRepository> _invoiceRepositoryMock;
    private readonly Mock<IBookingAvailabilityService> _availabilityServiceMock;
    private readonly Mock<IBookingPricingService> _pricingServiceMock;
    private readonly Mock<IBookingTransactionManager> _transactionManagerMock;
    private readonly Mock<IPaymentService> _paymentServiceMock;
    private readonly Mock<IBookingConfirmationEmailService> _emailServiceMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;

    private readonly CreateBookingsService _service;

    public CreateBookingsServiceTests()
    {
        _cartRepositoryMock = new Mock<ICartRepository>();
        _bookingRepositoryMock = new Mock<IBookingRepository>();
        _invoiceRepositoryMock = new Mock<IInvoiceRepository>();
        _availabilityServiceMock = new Mock<IBookingAvailabilityService>();
        _pricingServiceMock = new Mock<IBookingPricingService>();
        _transactionManagerMock = new Mock<IBookingTransactionManager>();
        _paymentServiceMock = new Mock<IPaymentService>();
        _emailServiceMock = new Mock<IBookingConfirmationEmailService>();
        _userRepositoryMock = new Mock<IUserRepository>();

        _transactionManagerMock
            .Setup(manager => manager.ExecuteSerializableAsync(It.IsAny<Func<Task>>()))
            .Returns((Func<Task> operation) => operation());

        _service = new CreateBookingsService(
            _cartRepositoryMock.Object,
            _bookingRepositoryMock.Object,
            _invoiceRepositoryMock.Object,
            _availabilityServiceMock.Object,
            _pricingServiceMock.Object,
            _transactionManagerMock.Object,
            _paymentServiceMock.Object,
            _emailServiceMock.Object,
            _userRepositoryMock.Object);
    }

    [Fact]
    public async Task CreateBookingsAsync_WhenUserIdIsInvalid_ShouldThrowBadRequestException()
    {
        // Arrange
        var paymentInformation = CreatePaymentInformation();

        // Act
        var action = async () => await _service.CreateBookingsAsync(0, null, paymentInformation);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        _userRepositoryMock.Verify(repository => repository.GetEmailByIdAsync(It.IsAny<int>()), Times.Never);
        _transactionManagerMock.Verify(manager => manager.ExecuteSerializableAsync(It.IsAny<Func<Task>>()), Times.Never);
    }

    [Fact]
    public async Task CreateBookingsAsync_WhenPaymentInformationIsNull_ShouldThrowBadRequestException()
    {
        // Act
        var action = async () => await _service.CreateBookingsAsync(1, null, null!);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        _userRepositoryMock.Verify(repository => repository.GetEmailByIdAsync(It.IsAny<int>()), Times.Never);
        _transactionManagerMock.Verify(manager => manager.ExecuteSerializableAsync(It.IsAny<Func<Task>>()), Times.Never);
    }

    [Fact]
    public async Task CreateBookingsAsync_WhenSpecialRequestsExceedMaximumLength_ShouldThrowBadRequestException()
    {
        // Arrange
        var paymentInformation = CreatePaymentInformation();
        var specialRequests = new string('a', 1001);

        // Act
        var action = async () => await _service.CreateBookingsAsync(1, specialRequests, paymentInformation);

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        _userRepositoryMock.Verify(repository => repository.GetEmailByIdAsync(It.IsAny<int>()), Times.Never);
        _transactionManagerMock.Verify(manager => manager.ExecuteSerializableAsync(It.IsAny<Func<Task>>()), Times.Never);
    }

    [Fact]
    public async Task CreateBookingsAsync_WhenCustomerEmailDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        const int userId = 1;

        _userRepositoryMock.Setup(repository => repository.GetEmailByIdAsync(userId)).ReturnsAsync((string?)null);

        // Act
        var action = async () => await _service.CreateBookingsAsync(userId, null, CreatePaymentInformation());

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);

        _transactionManagerMock.Verify(manager => manager.ExecuteSerializableAsync(It.IsAny<Func<Task>>()), Times.Never);
    }

    [Fact]
    public async Task CreateBookingsAsync_WhenCustomerEmailIsWhitespace_ShouldThrowNotFoundException()
    {
        // Arrange
        const int userId = 1;

        _userRepositoryMock.Setup(repository => repository.GetEmailByIdAsync(userId)).ReturnsAsync("   ");

        // Act
        var action = async () => await _service.CreateBookingsAsync(userId, null, CreatePaymentInformation());

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(action);

        _transactionManagerMock.Verify(manager => manager.ExecuteSerializableAsync(It.IsAny<Func<Task>>()), Times.Never);
    }

    [Fact]
    public async Task CreateBookingsAsync_WhenCartIsEmpty_ShouldThrowBadRequestException()
    {
        // Arrange
        const int userId = 1;

        SetupCustomerEmail(userId);

        _cartRepositoryMock.Setup(repository => repository.GetByUserIdAsync(userId)).ReturnsAsync(new List<CartItem>());

        // Act
        var action = async () => await _service.CreateBookingsAsync(userId, null, CreatePaymentInformation());

        // Assert
        await Assert.ThrowsAsync<BadRequestException>(action);

        _invoiceRepositoryMock.Verify(repository => repository.AddAsync(It.IsAny<Invoice>()), Times.Never);
        _bookingRepositoryMock.Verify(repository => repository.AddAsync(It.IsAny<Booking>()), Times.Never);
        _paymentServiceMock.Verify(service => service.ProcessPaymentAsync(It.IsAny<Invoice>(), It.IsAny<PaymentInformationDto>()), Times.Never);
    }

    [Fact]
    public async Task CreateBookingsAsync_WhenRoomIsNotAvailable_ShouldThrowConflictException()
    {
        // Arrange
        const int userId = 1;

        var cartItem = CreateCartItem(userId: userId, roomId: 10, hotelId: 100);

        SetupCustomerEmail(userId);

        _cartRepositoryMock.Setup(repository => repository.GetByUserIdAsync(userId)).ReturnsAsync(new List<CartItem> { cartItem });
        _availabilityServiceMock.Setup(service => service.IsRoomAvailableAsync(cartItem.RoomId, cartItem.CheckIn, cartItem.CheckOut, null)).ReturnsAsync(false);

        // Act
        var action = async () => await _service.CreateBookingsAsync(userId, null, CreatePaymentInformation());

        // Assert
        await Assert.ThrowsAsync<ConflictException>(action);

        _pricingServiceMock.Verify(service => service.CalculatePriceAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
        _invoiceRepositoryMock.Verify(repository => repository.AddAsync(It.IsAny<Invoice>()), Times.Never);
        _bookingRepositoryMock.Verify(repository => repository.AddAsync(It.IsAny<Booking>()), Times.Never);
    }

    [Fact]
    public async Task CreateBookingsAsync_WhenPaymentSucceeds_ShouldCreateBookingAndInvoice()
    {
        // Arrange
        const int userId = 1;

        var cartItem = CreateCartItem(userId: userId, roomId: 10, hotelId: 100);

        SetupSuccessfulCheckout(userId, new List<CartItem> { cartItem });

        // Act
        var result = await _service.CreateBookingsAsync(userId, "Late check-in", CreatePaymentInformation());

        // Assert
        Assert.Equal(1, result.BookingCount);
        Assert.Equal(1, result.InvoiceCount);

        _invoiceRepositoryMock.Verify(repository => repository.AddAsync(
                It.Is<Invoice>(invoice =>
                    invoice.UserId == userId &&
                    invoice.HotelId == 100 &&
                    invoice.TotalAmount == 200m)),
            Times.Once);

        _bookingRepositoryMock.Verify(repository => repository.AddAsync(
                It.Is<Booking>(booking =>
                    booking.UserId == userId &&
                    booking.RoomId == cartItem.RoomId &&
                    booking.TotalPrice == 200m &&
                    booking.SpecialRequests == "Late check-in")),
            Times.Once);
    }

    [Fact]
    public async Task CreateBookingsAsync_WhenPaymentSucceeds_ShouldDeleteCart()
    {
        // Arrange
        const int userId = 1;

        var cartItem = CreateCartItem(userId: userId, roomId: 10, hotelId: 100);
        var cartItems = new List<CartItem> { cartItem };

        SetupSuccessfulCheckout(userId, cartItems);

        // Act
        await _service.CreateBookingsAsync(userId, null, CreatePaymentInformation());

        // Assert
        _cartRepositoryMock.Verify(repository => repository.DeleteRange(cartItems), Times.Once);
    }

    [Fact]
    public async Task CreateBookingsAsync_WhenPaymentSucceeds_ShouldReturnPaymentResultAndConfirmation()
    {
        // Arrange
        const int userId = 1;

        var cartItem = CreateCartItem(userId: userId, roomId: 10, hotelId: 100);

        SetupSuccessfulCheckout(userId, new List<CartItem> { cartItem });

        // Act
        var result = await _service.CreateBookingsAsync(userId, null, CreatePaymentInformation());

        // Assert
        Assert.Single(result.Payments);
        Assert.Equal(PaymentStatus.Paid, result.Payments[0].Status);
        Assert.Equal(200m, result.Payments[0].Amount);
        Assert.Single(result.Confirmations);

        var confirmation = result.Confirmations[0];

        Assert.Equal(100, confirmation.HotelId);
        Assert.Equal("Test Hotel", confirmation.HotelName);
        Assert.Equal(200m, confirmation.TotalAmount);
        Assert.Equal(PaymentStatus.Paid, confirmation.PaymentStatus);
        Assert.Single(confirmation.Rooms);
        Assert.Equal(cartItem.RoomId, confirmation.Rooms[0].RoomId);
        Assert.Equal(cartItem.Room!.RoomNumber, confirmation.Rooms[0].RoomNumber);
    }

    [Fact]
    public async Task CreateBookingsAsync_WhenPaymentSucceeds_ShouldSendConfirmationEmail()
    {
        // Arrange
        const int userId = 1;

        var cartItem = CreateCartItem(userId: userId, roomId: 10, hotelId: 100);

        SetupSuccessfulCheckout(userId, new List<CartItem> { cartItem });

        // Act
        await _service.CreateBookingsAsync(userId, null, CreatePaymentInformation());

        // Assert
        _emailServiceMock.Verify(service => service.SendAsync(
                It.Is<BookingConfirmationEmailDto>(confirmation =>
                    confirmation.CustomerEmail == "customer@test.com" &&
                    confirmation.HotelName == "Test Hotel" &&
                    confirmation.InvoiceTotal == 200m &&
                    confirmation.PaymentStatus == PaymentStatus.Paid &&
                    confirmation.Rooms.Count == 1)),
            Times.Once);
    }

    [Fact]
    public async Task CreateBookingsAsync_WhenEmailSendingFails_ShouldStillReturnSuccessfulResult()
    {
        // Arrange
        const int userId = 1;

        var cartItem = CreateCartItem(userId: userId, roomId: 10, hotelId: 100);

        SetupSuccessfulCheckout(userId, new List<CartItem> { cartItem });

        _emailServiceMock.Setup(service => service.SendAsync(It.IsAny<BookingConfirmationEmailDto>())).ThrowsAsync(new Exception("SMTP failure"));

        // Act
        var result = await _service.CreateBookingsAsync(userId, null, CreatePaymentInformation());

        // Assert
        Assert.Equal(1, result.BookingCount);
        Assert.Equal(1, result.InvoiceCount);
        Assert.Single(result.Payments);
        Assert.Equal(PaymentStatus.Paid, result.Payments[0].Status);
        Assert.Single(result.Confirmations);
    }

    [Fact]
    public async Task CreateBookingsAsync_WhenPaymentFails_ShouldCancelBooking()
    {
        // Arrange
        const int userId = 1;

        var cartItem = CreateCartItem(userId: userId, roomId: 10, hotelId: 100);

        SetupCustomerEmail(userId);

        _cartRepositoryMock.Setup(repository => repository.GetByUserIdAsync(userId)).ReturnsAsync(new List<CartItem> { cartItem });

        SetupRoomAvailability(cartItem);
        SetupPricing(cartItem);

        Booking? createdBooking = null;

        _bookingRepositoryMock.Setup(repository => repository.AddAsync(It.IsAny<Booking>()))
            .Callback<Booking>(booking => createdBooking = booking)
            .Returns(Task.CompletedTask);

        _paymentServiceMock.Setup(service => service.ProcessPaymentAsync(It.IsAny<Invoice>(),
                    It.IsAny<PaymentInformationDto>()))
            .ReturnsAsync(() => CreateFailedPayment(200m));

        // Act
        var result = await _service.CreateBookingsAsync(userId, null, CreatePaymentInformation());

        // Assert
        Assert.NotNull(createdBooking);
        Assert.Equal(BookingStatus.Cancelled, createdBooking!.BookingStatus);
        Assert.Single(result.Payments);
        Assert.Equal(PaymentStatus.Failed, result.Payments[0].Status);
        Assert.Empty(result.Confirmations);
    }

    [Fact]
    public async Task CreateBookingsAsync_WhenPaymentFails_ShouldNotDeleteCartOrSendEmail()
    {
        // Arrange
        const int userId = 1;

        var cartItem = CreateCartItem(userId: userId, roomId: 10, hotelId: 100);

        SetupCustomerEmail(userId);

        _cartRepositoryMock.Setup(repository => repository.GetByUserIdAsync(userId)).ReturnsAsync(new List<CartItem> { cartItem });

        SetupRoomAvailability(cartItem);
        SetupPricing(cartItem);

        _paymentServiceMock.Setup(service => service.ProcessPaymentAsync(It.IsAny<Invoice>(),
                    It.IsAny<PaymentInformationDto>()))
            .ReturnsAsync(() => CreateFailedPayment(200m));

        // Act
        await _service.CreateBookingsAsync(userId, null, CreatePaymentInformation());

        // Assert
        _cartRepositoryMock.Verify(repository => repository.DeleteRange(It.IsAny<IEnumerable<CartItem>>()), Times.Never);
        _emailServiceMock.Verify(service => service.SendAsync(It.IsAny<BookingConfirmationEmailDto>()), Times.Never);
    }

    [Fact]
    public async Task CreateBookingsAsync_WhenCartContainsRoomsFromSameHotel_ShouldCreateOneInvoice()
    {
        // Arrange
        const int userId = 1;

        var firstItem = CreateCartItem(userId: userId, roomId: 10, hotelId: 100, roomNumber: "101");
        var secondItem = CreateCartItem(userId: userId, roomId: 11, hotelId: 100, roomNumber: "102");
        var cartItems = new List<CartItem> { firstItem, secondItem };

        SetupCustomerEmail(userId);

        _cartRepositoryMock.Setup(repository => repository.GetByUserIdAsync(userId)).ReturnsAsync(cartItems);

        SetupRoomAvailability(firstItem);
        SetupRoomAvailability(secondItem);
        SetupPricing(firstItem);
        SetupPricing(secondItem);

        _paymentServiceMock.Setup(service => service.ProcessPaymentAsync(
                    It.IsAny<Invoice>(),
                    It.IsAny<PaymentInformationDto>()))
            .ReturnsAsync((Invoice invoice, PaymentInformationDto _) => CreatePaidPayment(invoice.TotalAmount));

        // Act
        var result = await _service.CreateBookingsAsync(userId, null, CreatePaymentInformation());

        // Assert
        Assert.Equal(2, result.BookingCount);
        Assert.Equal(1, result.InvoiceCount);

        _invoiceRepositoryMock.Verify(repository => repository.AddAsync( It.IsAny<Invoice>()), Times.Once);
        _bookingRepositoryMock.Verify(repository => repository.AddAsync(It.IsAny<Booking>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CreateBookingsAsync_WhenCartContainsRoomsFromDifferentHotels_ShouldCreateSeparateInvoices()
    {
        // Arrange
        const int userId = 1;

        var firstItem = CreateCartItem(userId: userId, roomId: 10, hotelId: 100, hotelName: "First Hotel");
        var secondItem = CreateCartItem(userId: userId, roomId: 20, hotelId: 200, hotelName: "Second Hotel");
        var cartItems = new List<CartItem> { firstItem, secondItem };

        SetupCustomerEmail(userId);

        _cartRepositoryMock.Setup(repository => repository.GetByUserIdAsync(userId)).ReturnsAsync(cartItems);

        SetupRoomAvailability(firstItem);
        SetupRoomAvailability(secondItem);
        SetupPricing(firstItem);
        SetupPricing(secondItem);

        _paymentServiceMock.Setup(service => service.ProcessPaymentAsync(
                    It.IsAny<Invoice>(),
                    It.IsAny<PaymentInformationDto>()))
            .ReturnsAsync((Invoice invoice, PaymentInformationDto _) => CreatePaidPayment(invoice.TotalAmount));

        // Act
        var result = await _service.CreateBookingsAsync(userId, null, CreatePaymentInformation());

        // Assert
        Assert.Equal(2, result.BookingCount);
        Assert.Equal(2, result.InvoiceCount);
        Assert.Equal(2, result.Payments.Count);
        Assert.Equal(2, result.Confirmations.Count);

        _invoiceRepositoryMock.Verify(repository => repository.AddAsync(It.IsAny<Invoice>()), Times.Exactly(2));
        _paymentServiceMock.Verify(service => service.ProcessPaymentAsync(
                It.IsAny<Invoice>(),
                It.IsAny<PaymentInformationDto>()), Times.Exactly(2));

        _emailServiceMock.Verify(service => service.SendAsync(It.IsAny<BookingConfirmationEmailDto>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CreateBookingsAsync_WhenAnyPaymentFails_ShouldNotDeleteCart()
    {
        // Arrange
        const int userId = 1;

        var firstItem = CreateCartItem(userId: userId, roomId: 10, hotelId: 100, hotelName: "First Hotel");
        var secondItem = CreateCartItem(userId: userId, roomId: 20, hotelId: 200, hotelName: "Second Hotel");
        var cartItems = new List<CartItem> { firstItem, secondItem };

        SetupCustomerEmail(userId);

        _cartRepositoryMock.Setup(repository => repository.GetByUserIdAsync(userId)).ReturnsAsync(cartItems);

        SetupRoomAvailability(firstItem);
        SetupRoomAvailability(secondItem);
        SetupPricing(firstItem);
        SetupPricing(secondItem);

        var paymentCall = 0;

        _paymentServiceMock.Setup(service => service.ProcessPaymentAsync(It.IsAny<Invoice>(),
                    It.IsAny<PaymentInformationDto>()))
            .ReturnsAsync((Invoice invoice, PaymentInformationDto _) =>
            {
                paymentCall++;

                return paymentCall == 1 ? CreatePaidPayment(invoice.TotalAmount) : CreateFailedPayment(invoice.TotalAmount);
            });

        // Act
        var result = await _service.CreateBookingsAsync(userId, null, CreatePaymentInformation());

        // Assert
        Assert.Equal(2, result.Payments.Count);
        Assert.Contains(result.Payments, payment => payment.Status == PaymentStatus.Paid);
        Assert.Contains(result.Payments, payment => payment.Status == PaymentStatus.Failed);

        _cartRepositoryMock.Verify(repository => repository.DeleteRange(It.IsAny<IEnumerable<CartItem>>()), Times.Never);
    }

    private void SetupCustomerEmail(int userId)
    {
        _userRepositoryMock.Setup(repository => repository.GetEmailByIdAsync(userId)).ReturnsAsync("customer@test.com");
    }

    private void SetupSuccessfulCheckout(int userId, List<CartItem> cartItems)
    {
        SetupCustomerEmail(userId);

        _cartRepositoryMock.Setup(repository => repository.GetByUserIdAsync(userId)).ReturnsAsync(cartItems);

        foreach (var item in cartItems)
        {
            SetupRoomAvailability(item);
            SetupPricing(item);
        }

        _paymentServiceMock.Setup(service => service.ProcessPaymentAsync(It.IsAny<Invoice>(),
                    It.IsAny<PaymentInformationDto>()))
            .ReturnsAsync((Invoice invoice, PaymentInformationDto _) =>
                CreatePaidPayment(invoice.TotalAmount));
    }

    private void SetupRoomAvailability(CartItem item)
    {
        _availabilityServiceMock.Setup(service => service.IsRoomAvailableAsync(item.RoomId, item.CheckIn, item.CheckOut, null)).ReturnsAsync(true);
    }

    private void SetupPricing(CartItem item)
    {
        _pricingServiceMock
            .Setup(service => service.CalculatePriceAsync(item.RoomId, item.CheckIn, item.CheckOut, It.IsAny<DateTime>()))
            .ReturnsAsync(new BookingPriceResultDto
            {
                PricePerNight = 100m,
                NumberOfNights = 2,
                OriginalTotalPrice = 200m,
                DiscountPercentage = 0,
                DiscountAmount = 0m,
                TotalPrice = 200m
            });
    }

    private static CartItem CreateCartItem(int userId, int roomId, int hotelId, string roomNumber = "101", string hotelName = "Test Hotel")
    {
        var hotel = new Hotel(
            name: hotelName,
            ownerName: "Test Owner",
            address: "Test Address",
            latitude: 0,
            longitude: 0,
            hotelType: (HotelType)1,
            cityId: 1,
            description: null,
            history: null)
        {
            HotelId = hotelId
        };

        var room = new Room(
            roomNumber: roomNumber,
            roomType: (RoomType)1,
            pricePerNight: 100m,
            adultsCapacity: 2,
            childCapacity: 2,
            hotelId: hotelId,
            description: null)
        {
            RoomId = roomId,
            Hotel = hotel
        };

        var checkIn = DateTime.UtcNow.Date.AddDays(5);
        var checkOut = checkIn.AddDays(2);

        return new CartItem(
            userId: userId,
            roomId: roomId,
            checkIn: checkIn,
            checkOut: checkOut,
            adults: 2,
            children: 0)
        {
            Room = room
        };
    }

    private static PaymentInformationDto CreatePaymentInformation()
    {
        return new PaymentInformationDto
        {
            ShouldSucceed = true
        };
    }

    private static Payment CreatePaidPayment(decimal amount)
    {
        var payment = new Payment(amount);
        payment.MarkAsPaid();

        return payment;
    }

    private static Payment CreateFailedPayment(decimal amount)
    {
        var payment = new Payment(amount);
        payment.MarkAsFailed();

        return payment;
    }
}