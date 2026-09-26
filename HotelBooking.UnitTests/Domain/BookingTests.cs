using HotelBooking.Domain.Entities;

namespace HotelBooking.UnitTests.Domain.Entities;

public class BookingTests
{
    [Fact]
    public void Constructor_WhenDataIsValid_ShouldCreateBookingWithCorrectValues()
    {
        // Arrange
        var checkIn = new DateTime(2026, 10, 10);
        var checkOut = new DateTime(2026, 10, 13);

        var beforeCreation = DateTime.UtcNow;

        // Act
        var booking = new Booking(
            userId: 1,
            roomId: 10,
            checkIn: checkIn,
            checkOut: checkOut,
            adults: 2,
            children: 1,
            pricePerNight: 100m,
            originalTotalPrice: 300m,
            discountPercentage: 10,
            discountAmount: 30m,
            totalPrice: 270m,
            specialRequests: "Late check-in");

        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(1, booking.UserId);
        Assert.Equal(10, booking.RoomId);
        Assert.Equal(checkIn, booking.CheckIn);
        Assert.Equal(checkOut, booking.CheckOut);
        Assert.Equal(2, booking.Adults);
        Assert.Equal(1, booking.Children);
        Assert.Equal(100m, booking.PricePerNight);
        Assert.Equal(300m, booking.OriginalTotalPrice);
        Assert.Equal(10, booking.DiscountPercentage);
        Assert.Equal(30m, booking.DiscountAmount);
        Assert.Equal(270m, booking.TotalPrice);
        Assert.Equal("Late check-in", booking.SpecialRequests);

        Assert.Equal(BookingStatus.Pending, booking.BookingStatus);
        Assert.Null(booking.UpdatedAt);

        Assert.InRange(
            booking.CreatedAt,
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
        var action = () => CreateValidBooking(userId: userId);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenRoomIdIsInvalid_ShouldThrowArgumentOutOfRangeException(
        int roomId)
    {
        // Act
        var action = () => CreateValidBooking(roomId: roomId);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void Constructor_WhenCheckOutIsBeforeCheckIn_ShouldThrowArgumentException()
    {
        // Act
        var action = () => CreateValidBooking(
            checkIn: new DateTime(2026, 10, 10),
            checkOut: new DateTime(2026, 10, 9));

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Constructor_WhenCheckOutEqualsCheckIn_ShouldThrowArgumentException()
    {
        // Arrange
        var date = new DateTime(2026, 10, 10);

        // Act
        var action = () => CreateValidBooking(
            checkIn: date,
            checkOut: date);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenAdultsIsInvalid_ShouldThrowArgumentOutOfRangeException(
        int adults)
    {
        // Act
        var action = () => CreateValidBooking(adults: adults);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void Constructor_WhenChildrenIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        // Act
        var action = () => CreateValidBooking(children: -1);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenPricePerNightIsInvalid_ShouldThrowArgumentOutOfRangeException(
        int pricePerNight)
    {
        // Act
        var action = () =>
            CreateValidBooking(pricePerNight: pricePerNight);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenOriginalTotalPriceIsInvalid_ShouldThrowArgumentOutOfRangeException(
        int originalTotalPrice)
    {
        // Act
        var action = () =>
            CreateValidBooking(originalTotalPrice: originalTotalPrice);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100)]
    public void Constructor_WhenDiscountPercentageIsInvalid_ShouldThrowArgumentOutOfRangeException(
        int discountPercentage)
    {
        // Act
        var action = () =>
            CreateValidBooking(
                discountPercentage: discountPercentage);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void Constructor_WhenDiscountAmountIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        // Act
        var action = () =>
            CreateValidBooking(discountAmount: -1m);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenTotalPriceIsInvalid_ShouldThrowArgumentOutOfRangeException(
        int totalPrice)
    {
        // Act
        var action = () =>
            CreateValidBooking(totalPrice: totalPrice);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void Constructor_WhenSpecialRequestsExceed1000Characters_ShouldThrowArgumentException()
    {
        // Arrange
        var specialRequests = new string('A', 1001);

        // Act
        var action = () =>
            CreateValidBooking(
                specialRequests: specialRequests);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Constructor_WhenSpecialRequestsAreExactly1000Characters_ShouldSucceed()
    {
        // Arrange
        var specialRequests = new string('A', 1000);

        // Act
        var booking = CreateValidBooking(
            specialRequests: specialRequests);

        // Assert
        Assert.Equal(specialRequests, booking.SpecialRequests);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void Constructor_WhenDiscountPercentageIsAtValidBoundary_ShouldSucceed(
        int discountPercentage)
    {
        // Act
        var booking = CreateValidBooking(
            discountPercentage: discountPercentage);

        // Assert
        Assert.Equal(
            discountPercentage,
            booking.DiscountPercentage);
    }

    [Fact]
    public void Cancel_ShouldChangeStatusToCancelled()
    {
        // Arrange
        var booking = CreateValidBooking();

        // Act
        booking.Cancel();

        // Assert
        Assert.Equal(
            BookingStatus.Cancelled,
            booking.BookingStatus);
    }

    [Fact]
    public void Cancel_ShouldSetUpdatedAt()
    {
        // Arrange
        var booking = CreateValidBooking();

        var beforeCancel = DateTime.UtcNow;

        // Act
        booking.Cancel();

        var afterCancel = DateTime.UtcNow;

        // Assert
        Assert.NotNull(booking.UpdatedAt);

        Assert.InRange(
            booking.UpdatedAt!.Value,
            beforeCancel,
            afterCancel);
    }

    [Fact]
    public void Modify_WhenDataIsValid_ShouldUpdateAllModifiableFields()
    {
        // Arrange
        var booking = CreateValidBooking();

        var newCheckIn = new DateTime(2026, 11, 1);
        var newCheckOut = new DateTime(2026, 11, 5);

        // Act
        booking.Modify(
            roomId: 20,
            checkIn: newCheckIn,
            checkOut: newCheckOut,
            adults: 3,
            children: 2,
            pricePerNight: 150m,
            originalTotalPrice: 600m,
            discountPercentage: 20,
            discountAmount: 120m,
            totalPrice: 480m,
            specialRequests: "Updated request");

        // Assert
        Assert.Equal(20, booking.RoomId);
        Assert.Equal(newCheckIn, booking.CheckIn);
        Assert.Equal(newCheckOut, booking.CheckOut);
        Assert.Equal(3, booking.Adults);
        Assert.Equal(2, booking.Children);
        Assert.Equal(150m, booking.PricePerNight);
        Assert.Equal(600m, booking.OriginalTotalPrice);
        Assert.Equal(20, booking.DiscountPercentage);
        Assert.Equal(120m, booking.DiscountAmount);
        Assert.Equal(480m, booking.TotalPrice);
        Assert.Equal(
            "Updated request",
            booking.SpecialRequests);
    }

    [Fact]
    public void Modify_WhenDataIsValid_ShouldSetUpdatedAt()
    {
        // Arrange
        var booking = CreateValidBooking();

        var beforeModify = DateTime.UtcNow;

        // Act
        ModifyWithValidData(booking);

        var afterModify = DateTime.UtcNow;

        // Assert
        Assert.NotNull(booking.UpdatedAt);

        Assert.InRange(
            booking.UpdatedAt!.Value,
            beforeModify,
            afterModify);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Modify_WhenRoomIdIsInvalid_ShouldThrowArgumentException(
        int roomId)
    {
        // Arrange
        var booking = CreateValidBooking();

        // Act
        var action = () =>
            ModifyWithValidData(
                booking,
                roomId: roomId);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Modify_WhenCheckOutIsBeforeCheckIn_ShouldThrowArgumentException()
    {
        // Arrange
        var booking = CreateValidBooking();

        // Act
        var action = () =>
            ModifyWithValidData(
                booking,
                checkIn: new DateTime(2026, 11, 10),
                checkOut: new DateTime(2026, 11, 9));

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Modify_WhenCheckOutEqualsCheckIn_ShouldThrowArgumentException()
    {
        // Arrange
        var booking = CreateValidBooking();
        var date = new DateTime(2026, 11, 10);

        // Act
        var action = () =>
            ModifyWithValidData(
                booking,
                checkIn: date,
                checkOut: date);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Modify_WhenAdultsIsInvalid_ShouldThrowArgumentException(
        int adults)
    {
        // Arrange
        var booking = CreateValidBooking();

        // Act
        var action = () =>
            ModifyWithValidData(
                booking,
                adults: adults);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Modify_WhenChildrenIsNegative_ShouldThrowArgumentException()
    {
        // Arrange
        var booking = CreateValidBooking();

        // Act
        var action = () =>
            ModifyWithValidData(
                booking,
                children: -1);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Modify_WhenPricePerNightIsInvalid_ShouldThrowArgumentException(
        int pricePerNight)
    {
        // Arrange
        var booking = CreateValidBooking();

        // Act
        var action = () =>
            ModifyWithValidData(
                booking,
                pricePerNight: pricePerNight);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Modify_WhenOriginalTotalPriceIsInvalid_ShouldThrowArgumentException(
        int originalTotalPrice)
    {
        // Arrange
        var booking = CreateValidBooking();

        // Act
        var action = () =>
            ModifyWithValidData(
                booking,
                originalTotalPrice: originalTotalPrice);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100)]
    public void Modify_WhenDiscountPercentageIsInvalid_ShouldThrowArgumentException(
        int discountPercentage)
    {
        // Arrange
        var booking = CreateValidBooking();

        // Act
        var action = () =>
            ModifyWithValidData(
                booking,
                discountPercentage: discountPercentage);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Modify_WhenDiscountAmountIsNegative_ShouldThrowArgumentException()
    {
        // Arrange
        var booking = CreateValidBooking();

        // Act
        var action = () =>
            ModifyWithValidData(
                booking,
                discountAmount: -1m);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Modify_WhenTotalPriceIsInvalid_ShouldThrowArgumentException(
        int totalPrice)
    {
        // Arrange
        var booking = CreateValidBooking();

        // Act
        var action = () =>
            ModifyWithValidData(
                booking,
                totalPrice: totalPrice);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Modify_WhenSpecialRequestsExceed1000Characters_ShouldThrowArgumentException()
    {
        // Arrange
        var booking = CreateValidBooking();

        // Act
        var action = () =>
            ModifyWithValidData(
                booking,
                specialRequests: new string('A', 1001));

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Modify_WhenSpecialRequestsAreExactly1000Characters_ShouldSucceed()
    {
        // Arrange
        var booking = CreateValidBooking();
        var specialRequests = new string('A', 1000);

        // Act
        ModifyWithValidData(
            booking,
            specialRequests: specialRequests);

        // Assert
        Assert.Equal(
            specialRequests,
            booking.SpecialRequests);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void Modify_WhenDiscountPercentageIsAtValidBoundary_ShouldSucceed(
        int discountPercentage)
    {
        // Arrange
        var booking = CreateValidBooking();

        // Act
        ModifyWithValidData(
            booking,
            discountPercentage: discountPercentage);

        // Assert
        Assert.Equal(
            discountPercentage,
            booking.DiscountPercentage);
    }

    private static Booking CreateValidBooking(
        int userId = 1,
        int roomId = 10,
        DateTime? checkIn = null,
        DateTime? checkOut = null,
        int adults = 2,
        int children = 1,
        decimal pricePerNight = 100m,
        decimal originalTotalPrice = 300m,
        int discountPercentage = 10,
        decimal discountAmount = 30m,
        decimal totalPrice = 270m,
        string? specialRequests = "Test request")
    {
        return new Booking(
            userId,
            roomId,
            checkIn ?? new DateTime(2026, 10, 10),
            checkOut ?? new DateTime(2026, 10, 13),
            adults,
            children,
            pricePerNight,
            originalTotalPrice,
            discountPercentage,
            discountAmount,
            totalPrice,
            specialRequests);
    }

    private static void ModifyWithValidData(
        Booking booking,
        int roomId = 20,
        DateTime? checkIn = null,
        DateTime? checkOut = null,
        int adults = 3,
        int children = 1,
        decimal pricePerNight = 150m,
        decimal originalTotalPrice = 450m,
        int discountPercentage = 10,
        decimal discountAmount = 45m,
        decimal totalPrice = 405m,
        string? specialRequests = "Updated request")
    {
        booking.Modify(
            roomId,
            checkIn ?? new DateTime(2026, 11, 10),
            checkOut ?? new DateTime(2026, 11, 13),
            adults,
            children,
            pricePerNight,
            originalTotalPrice,
            discountPercentage,
            discountAmount,
            totalPrice,
            specialRequests);
    }
}