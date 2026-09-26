using HotelBooking.Application.Bookings;
using HotelBooking.Application.Bookings.Dtos;
using HotelBooking.Application.Bookings.Modify;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using Moq;

namespace HotelBooking.UnitTests.Bookings.Modify;

public class ModifyBookingServiceTests
{
    private readonly Mock<IBookingRepository> _bookingRepositoryMock;
    private readonly Mock<IBookingAvailabilityService> _availabilityServiceMock;
    private readonly Mock<IBookingPricingService> _pricingServiceMock;
    private readonly Mock<IBookingTransactionManager> _transactionManagerMock;
    private readonly Mock<IRoomRepository> _roomRepositoryMock;
    private readonly ModifyBookingService _service;

    public ModifyBookingServiceTests()
    {
        _bookingRepositoryMock = new Mock<IBookingRepository>();
        _availabilityServiceMock = new Mock<IBookingAvailabilityService>();
        _pricingServiceMock = new Mock<IBookingPricingService>();
        _transactionManagerMock = new Mock<IBookingTransactionManager>();
        _roomRepositoryMock = new Mock<IRoomRepository>();
        _transactionManagerMock.Setup(manager => manager.ExecuteSerializableAsync(It.IsAny<Func<Task>>())).Returns((Func<Task> operation) => operation());

        _service = new ModifyBookingService(
            _bookingRepositoryMock.Object,
            _availabilityServiceMock.Object,
            _pricingServiceMock.Object,
            _transactionManagerMock.Object,
            _roomRepositoryMock.Object);
    }

    [Fact]
    public async Task ModifyAsync_WhenBookingIdIsInvalid_ShouldThrowBadRequestException()
    {
        var request = CreateValidRequest();
        var action = async () => await _service.ModifyAsync(0, 1, request);

        await Assert.ThrowsAsync<BadRequestException>(action);

        _transactionManagerMock.Verify(manager => manager.ExecuteSerializableAsync(It.IsAny<Func<Task>>()), Times.Never);
    }

    [Fact]
    public async Task ModifyAsync_WhenUserIdIsInvalid_ShouldThrowBadRequestException()
    {
        var request = CreateValidRequest();
        var action = async () => await _service.ModifyAsync(1, 0, request);

        await Assert.ThrowsAsync<BadRequestException>(action);

        _transactionManagerMock.Verify(manager => manager.ExecuteSerializableAsync(It.IsAny<Func<Task>>()), Times.Never);
    }

    [Fact]
    public async Task ModifyAsync_WhenBookingDoesNotExist_ShouldThrowNotFoundException()
    {
        var request = CreateValidRequest();

        _bookingRepositoryMock.Setup(repository => repository.GetByIdForUserAsync(1, 1)).ReturnsAsync((Booking?)null);

        var action = async () => await _service.ModifyAsync(1, 1, request);

        await Assert.ThrowsAsync<NotFoundException>(action);
    }

    [Fact]
    public async Task ModifyAsync_WhenStayHasAlreadyStarted_ShouldThrowConflictException()
    {
        var booking = CreateBooking(checkIn: DateTime.UtcNow.AddDays(-1), checkOut: DateTime.UtcNow.AddDays(2));
        var request = CreateValidRequest();

        _bookingRepositoryMock.Setup(repository => repository.GetByIdForUserAsync(1, 1)).ReturnsAsync(booking);

        var action = async () => await _service.ModifyAsync(1, 1, request);

        await Assert.ThrowsAsync<ConflictException>(action);

        _roomRepositoryMock.Verify(repository => repository.GetRoomByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ModifyAsync_WhenRoomIdIsInvalid_ShouldThrowBadRequestException()
    {
        var booking = CreateBooking();
        var request = CreateValidRequest();
        request.RoomId = 0;

        SetupBooking(booking);

        var action = async () => await _service.ModifyAsync(1, 1, request);

        await Assert.ThrowsAsync<BadRequestException>(action);
    }

    [Fact]
    public async Task ModifyAsync_WhenCheckOutIsNotAfterCheckIn_ShouldThrowBadRequestException()
    {
        var booking = CreateBooking();
        var request = CreateValidRequest();
        request.CheckOut = request.CheckIn;

        SetupBooking(booking);

        var action = async () => await _service.ModifyAsync(1, 1, request);

        await Assert.ThrowsAsync<BadRequestException>(action);
    }

    [Fact]
    public async Task ModifyAsync_WhenAdultsIsLessThanOne_ShouldThrowBadRequestException()
    {
        var booking = CreateBooking();
        var request = CreateValidRequest();
        request.Adults = 0;

        SetupBooking(booking);

        var action = async () => await _service.ModifyAsync(1, 1, request);

        await Assert.ThrowsAsync<BadRequestException>(action);
    }

    [Fact]
    public async Task ModifyAsync_WhenChildrenIsNegative_ShouldThrowBadRequestException()
    {
        var booking = CreateBooking();
        var request = CreateValidRequest();
        request.Children = -1;

        SetupBooking(booking);

        var action = async () => await _service.ModifyAsync(1, 1, request);

        await Assert.ThrowsAsync<BadRequestException>(action);
    }

    [Fact]
    public async Task ModifyAsync_WhenSpecialRequestsExceedMaximumLength_ShouldThrowBadRequestException()
    {
        var booking = CreateBooking();
        var request = CreateValidRequest();
        request.SpecialRequests = new string('a', 1001);

        SetupBooking(booking);

        var action = async () => await _service.ModifyAsync(1, 1, request);

        await Assert.ThrowsAsync<BadRequestException>(action);
    }

    [Fact]
    public async Task ModifyAsync_WhenInvoiceIsNotLoaded_ShouldThrowInvalidOperationException()
    {
        var booking = CreateBooking();
        booking.Invoice = null;

        var request = CreateValidRequest();

        SetupBooking(booking);

        var action = async () => await _service.ModifyAsync(1, 1, request);

        await Assert.ThrowsAsync<InvalidOperationException>(action);

        _roomRepositoryMock.Verify(repository => repository.GetRoomByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ModifyAsync_WhenRequestedRoomDoesNotExist_ShouldThrowNotFoundException()
    {
        var booking = CreateBooking();
        var request = CreateValidRequest();

        SetupBooking(booking);

        _roomRepositoryMock.Setup(repository => repository.GetRoomByIdAsync(request.RoomId)).ReturnsAsync((Room?)null);

        var action = async () => await _service.ModifyAsync(1, 1, request);

        await Assert.ThrowsAsync<NotFoundException>(action);
    }

    [Fact]
    public async Task ModifyAsync_WhenRequestedRoomIsInactive_ShouldThrowConflictException()
    {
        var booking = CreateBooking();
        var request = CreateValidRequest();

        var room = CreateRoom();
        room.ChangeStatus(false);

        SetupBooking(booking);

        _roomRepositoryMock.Setup(repository => repository.GetRoomByIdAsync(request.RoomId)).ReturnsAsync(room);

        var action = async () => await _service.ModifyAsync(1, 1, request);

        await Assert.ThrowsAsync<ConflictException>(action);
    }

    [Fact]
    public async Task ModifyAsync_WhenRequestedRoomIsOperationallyUnavailable_ShouldThrowConflictException()
    {
        var booking = CreateBooking();
        var request = CreateValidRequest();

        var room = CreateRoom();
        room.ChangeOperationalAvailability(false);

        SetupBooking(booking);

        _roomRepositoryMock.Setup(repository => repository.GetRoomByIdAsync(request.RoomId)).ReturnsAsync(room);

        var action = async () => await _service.ModifyAsync(1, 1, request);

        await Assert.ThrowsAsync<ConflictException>(action);
    }

    [Fact]
    public async Task ModifyAsync_WhenAdultsExceedRoomCapacity_ShouldThrowBadRequestException()
    {
        var booking = CreateBooking();
        var request = CreateValidRequest();
        request.Adults = 3;

        var room = CreateRoom();

        SetupBookingAndRoom(booking, room, request.RoomId);

        var action = async () => await _service.ModifyAsync(1, 1, request);

        await Assert.ThrowsAsync<BadRequestException>(action);
    }

    [Fact]
    public async Task ModifyAsync_WhenChildrenExceedRoomCapacity_ShouldThrowBadRequestException()
    {
        var booking = CreateBooking();
        var request = CreateValidRequest();
        request.Children = 3;

        var room = CreateRoom();

        SetupBookingAndRoom(booking, room, request.RoomId);

        var action = async () => await _service.ModifyAsync(1, 1, request);

        await Assert.ThrowsAsync<BadRequestException>(action);
    }

    [Fact]
    public async Task ModifyAsync_WhenRequestedRoomBelongsToDifferentHotel_ShouldThrowBadRequestException()
    {
        var booking = CreateBooking();
        var request = CreateValidRequest();
        var room = CreateRoom(hotelId: 2);

        SetupBookingAndRoom(booking, room, request.RoomId);

        var action = async () => await _service.ModifyAsync(1, 1, request);

        await Assert.ThrowsAsync<BadRequestException>(action);

        _availabilityServiceMock.Verify(service => service.IsRoomAvailableAsync(
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<int?>()),
            Times.Never);
    }

    [Fact]
    public async Task ModifyAsync_WhenRoomOrDatesChangedAndRoomIsUnavailable_ShouldThrowConflictException()
    {
        var booking = CreateBooking();
        var request = CreateValidRequest();
        request.RoomId = 2;

        var room = CreateRoom(roomId: 2);

        SetupBookingAndRoom(booking, room, request.RoomId);

        _availabilityServiceMock
            .Setup(service => service.IsRoomAvailableAsync(request.RoomId, request.CheckIn, request.CheckOut, booking.BookingId))
            .ReturnsAsync(false);

        var action = async () => await _service.ModifyAsync(1, 1, request);

        await Assert.ThrowsAsync<ConflictException>(action);

        _pricingServiceMock.Verify(service => service.CalculatePriceAsync(
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()),
            Times.Never);
    }

    [Fact]
    public async Task ModifyAsync_WhenRoomOrDatesChanged_ShouldCheckAvailabilityAndRecalculatePrice()
    {
        var booking = CreateBooking();
        var request = CreateValidRequest();
        request.RoomId = 2;

        var room = CreateRoom(roomId: 2);

        SetupBookingAndRoom(booking, room, request.RoomId);

        _availabilityServiceMock.Setup(service => service.IsRoomAvailableAsync(request.RoomId, request.CheckIn, request.CheckOut, booking.BookingId)).ReturnsAsync(true);

        _pricingServiceMock.Setup(service => service.CalculatePriceAsync(request.RoomId, request.CheckIn, request.CheckOut, It.IsAny<DateTime>()))
            .ReturnsAsync(new BookingPriceResultDto
            {
                PricePerNight = 150m,
                NumberOfNights = 2,
                OriginalTotalPrice = 300m,
                DiscountPercentage = 10,
                DiscountAmount = 30m,
                TotalPrice = 270m
            });

        await _service.ModifyAsync(1, 1, request);

        Assert.Equal(2, booking.RoomId);
        Assert.Equal(150m, booking.PricePerNight);
        Assert.Equal(300m, booking.OriginalTotalPrice);
        Assert.Equal(10, booking.DiscountPercentage);
        Assert.Equal(30m, booking.DiscountAmount);
        Assert.Equal(270m, booking.TotalPrice);

        _availabilityServiceMock.Verify(service => service.IsRoomAvailableAsync(request.RoomId, request.CheckIn, request.CheckOut, booking.BookingId), Times.Once);
        _pricingServiceMock.Verify(service => service.CalculatePriceAsync(request.RoomId, request.CheckIn, request.CheckOut, It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task ModifyAsync_WhenRoomAndDatesDidNotChange_ShouldKeepExistingPrice()
    {
        var booking = CreateBooking();
        var request = new ModifyBookingRequestDto
        {
            RoomId = booking.RoomId,
            CheckIn = booking.CheckIn,
            CheckOut = booking.CheckOut,
            Adults = 1,
            Children = 1,
            SpecialRequests = "Updated request"
        };

        var room = CreateRoom(roomId: booking.RoomId);

        SetupBookingAndRoom(booking, room, request.RoomId);

        var originalPricePerNight = booking.PricePerNight;
        var originalTotal = booking.TotalPrice;
        var originalDiscount = booking.DiscountPercentage;

        await _service.ModifyAsync(1, 1, request);

        Assert.Equal(originalPricePerNight, booking.PricePerNight);
        Assert.Equal(originalTotal, booking.TotalPrice);
        Assert.Equal(originalDiscount, booking.DiscountPercentage);
        Assert.Equal(1, booking.Adults);
        Assert.Equal(1, booking.Children);
        Assert.Equal("Updated request", booking.SpecialRequests);

        _availabilityServiceMock.Verify(service => service.IsRoomAvailableAsync(
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<int?>()),
            Times.Never);

        _pricingServiceMock.Verify(service => service.CalculatePriceAsync(
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()),
            Times.Never);
    }

    [Fact]
    public async Task ModifyAsync_WhenModificationSucceeds_ShouldUpdateInvoiceTotal()
    {
        var booking = CreateBooking();

        var secondBooking = new Booking(
            userId: 1,
            roomId: 3,
            checkIn: DateTime.UtcNow.AddDays(5),
            checkOut: DateTime.UtcNow.AddDays(7),
            adults: 1,
            children: 0,
            pricePerNight: 50m,
            originalTotalPrice: 100m,
            discountPercentage: 0,
            discountAmount: 0m,
            totalPrice: 100m,
            specialRequests: null);

        booking.Invoice!.Bookings.Add(secondBooking);

        var request = CreateValidRequest();
        request.RoomId = 2;

        var room = CreateRoom(roomId: 2);

        SetupBookingAndRoom(booking, room, request.RoomId);

        _availabilityServiceMock.Setup(service => service.IsRoomAvailableAsync(request.RoomId, request.CheckIn, request.CheckOut, booking.BookingId)).ReturnsAsync(true);

        _pricingServiceMock.Setup(service => service.CalculatePriceAsync(request.RoomId, request.CheckIn, request.CheckOut, It.IsAny<DateTime>()))
            .ReturnsAsync(new BookingPriceResultDto
            {
                PricePerNight = 150m,
                NumberOfNights = 2,
                OriginalTotalPrice = 300m,
                DiscountPercentage = 10,
                DiscountAmount = 30m,
                TotalPrice = 270m
            });

        await _service.ModifyAsync(1, 1, request);

        Assert.Equal(370m, booking.Invoice.TotalAmount);
    }

    private void SetupBooking(Booking booking)
    {
        _bookingRepositoryMock
            .Setup(repository => repository.GetByIdForUserAsync(1, 1))
            .ReturnsAsync(booking);
    }

    private void SetupBookingAndRoom(
        Booking booking,
        Room room,
        int roomId)
    {
        SetupBooking(booking);

        _roomRepositoryMock
            .Setup(repository =>
                repository.GetRoomByIdAsync(roomId))
            .ReturnsAsync(room);
    }

    private static Booking CreateBooking(
        DateTime? checkIn = null,
        DateTime? checkOut = null)
    {
        var booking = new Booking(
            userId: 1,
            roomId: 1,
            checkIn: checkIn ?? DateTime.UtcNow.AddDays(5),
            checkOut: checkOut ?? DateTime.UtcNow.AddDays(7),
            adults: 2,
            children: 0,
            pricePerNight: 100m,
            originalTotalPrice: 200m,
            discountPercentage: 0,
            discountAmount: 0m,
            totalPrice: 200m,
            specialRequests: null)
        {
            BookingId = 1
        };

        var invoice = new Invoice(
            userId: 1,
            hotelId: 1,
            totalAmount: 200m)
        {
            InvoiceId = 1
        };

        booking.Invoice = invoice;
        booking.InvoiceId = invoice.InvoiceId;

        invoice.Bookings.Add(booking);

        return booking;
    }

    private static Room CreateRoom(int roomId = 1, int hotelId = 1)
    {
        return new Room(
            roomNumber: "101",
            roomType: (RoomType)1,
            pricePerNight: 100m,
            adultsCapacity: 2,
            childCapacity: 2,
            hotelId: hotelId,
            description: null)
        {
            RoomId = roomId
        };
    }

    private static ModifyBookingRequestDto CreateValidRequest()
    {
        return new ModifyBookingRequestDto
        {
            RoomId = 1,
            CheckIn = DateTime.UtcNow.AddDays(6),
            CheckOut = DateTime.UtcNow.AddDays(8),
            Adults = 2,
            Children = 0,
            SpecialRequests = null
        };
    }
}