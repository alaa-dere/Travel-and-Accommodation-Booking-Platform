using HotelBooking.Application.Bookings.Dtos;
using HotelBooking.Application.Bookings.Modify;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using Moq;

namespace HotelBooking.UnitTests.Bookings.Modify;

public class ModifyBookingServiceTests
{
    private readonly Mock<IBookingRepository> _bookingRepository = new();
    private readonly Mock<IRoomRepository> _roomRepository = new();
    private readonly ModifyBookingService _service;

    public ModifyBookingServiceTests()
    {
        _service = new ModifyBookingService(
            _bookingRepository.Object,
            _roomRepository.Object,
            TimeProvider.System);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    public async Task ModifyAsync_WhenIdentifierIsInvalid_ShouldThrowBadRequest(int bookingId, int userId)
    {
        var action = () => _service.ModifyAsync(bookingId, userId, ValidRequest());

        await Assert.ThrowsAsync<BadRequestException>(action);
    }

    [Fact]
    public async Task ModifyAsync_WhenBookingDoesNotBelongToUser_ShouldThrowNotFound()
    {
        _bookingRepository
            .Setup(repository => repository.GetByIdForUserAsync(1, 1))
            .ReturnsAsync((Booking?)null);

        var action = () => _service.ModifyAsync(1, 1, ValidRequest());

        await Assert.ThrowsAsync<NotFoundException>(action);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ModifyAsync_WhenBookingCannotBeModified_ShouldThrowConflict(bool cancelled)
    {
        var booking = CreateBooking(cancelled ? DateTime.UtcNow.AddDays(2) : DateTime.UtcNow.AddDays(-1));
        if (cancelled)
        {
            booking.Cancel(DateTime.UtcNow);
        }
        SetupBooking(booking);

        var action = () => _service.ModifyAsync(1, 1, ValidRequest());

        await Assert.ThrowsAsync<ConflictException>(action);
    }

    [Fact]
    public async Task ModifyAsync_WhenRoomIsMissing_ShouldThrowInvalidOperationException()
    {
        SetupBooking(CreateBooking());
        _roomRepository
            .Setup(repository => repository.GetRoomByIdAsync(1))
            .ReturnsAsync((Room?)null);

        var action = () => _service.ModifyAsync(1, 1, ValidRequest());

        await Assert.ThrowsAsync<InvalidOperationException>(action);
    }

    [Theory]
    [InlineData(3, 0)]
    [InlineData(1, 3)]
    public async Task ModifyAsync_WhenGuestCountExceedsCapacity_ShouldThrowBadRequest(int adults, int children)
    {
        SetupBookingAndRoom(CreateBooking(), CreateRoom());
        var request = new ModifyBookingRequestDto
        {
            Adults = adults,
            Children = children
        };

        var action = () => _service.ModifyAsync(1, 1, request);

        await Assert.ThrowsAsync<BadRequestException>(action);
        _bookingRepository.Verify(repository => repository.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task ModifyAsync_WhenValid_ShouldUpdateOnlyGuestDetailsAndSave()
    {
        var booking = CreateBooking();
        var originalRoomId = booking.RoomId;
        var originalCheckIn = booking.CheckIn;
        var originalCheckOut = booking.CheckOut;
        var originalTotal = booking.TotalPrice;
        SetupBookingAndRoom(booking, CreateRoom());
        var request = new ModifyBookingRequestDto
        {
            Adults = 1,
            Children = 2,
            SpecialRequests = "  Late arrival  "
        };

        await _service.ModifyAsync(1, 1, request);

        Assert.Equal(1, booking.Adults);
        Assert.Equal(2, booking.Children);
        Assert.Equal("Late arrival", booking.SpecialRequests);
        Assert.Equal(originalRoomId, booking.RoomId);
        Assert.Equal(originalCheckIn, booking.CheckIn);
        Assert.Equal(originalCheckOut, booking.CheckOut);
        Assert.Equal(originalTotal, booking.TotalPrice);
        _bookingRepository.Verify(repository => repository.SaveChangesAsync(), Times.Once);
    }

    private void SetupBooking(Booking booking)
    {
        _bookingRepository
            .Setup(repository => repository.GetByIdForUserAsync(1, 1))
            .ReturnsAsync(booking);
    }

    private void SetupBookingAndRoom(Booking booking, Room room)
    {
        SetupBooking(booking);
        _roomRepository
            .Setup(repository => repository.GetRoomByIdAsync(booking.RoomId))
            .ReturnsAsync(room);
    }

    private static Booking CreateBooking(DateTime? checkIn = null)
    {
        var booking = BookingTestFactory.Create(
            1,
            1,
            checkIn ?? DateTime.UtcNow.AddDays(5),
            DateTime.UtcNow.AddDays(7),
            2,
            0,
            100m,
            200m,
            0,
            0m,
            200m,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow.AddMinutes(15));
        booking.BookingId = 1;
        return booking;
    }

    private static Room CreateRoom()
    {
        return new Room("101", RoomType.Double, 100m, 2, 2, 1, null)
        {
            RoomId = 1
        };
    }

    private static ModifyBookingRequestDto ValidRequest() => new()
    {
        Adults = 2,
        Children = 0
    };
}
