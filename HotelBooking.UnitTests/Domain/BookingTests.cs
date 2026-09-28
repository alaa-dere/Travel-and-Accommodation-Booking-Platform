using HotelBooking.Domain.Entities;
using HotelBooking.Domain.ValueObjects;

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
        var booking = BookingTestFactory.Create(
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
            specialRequests: "Late check-in",
            createdAt: DateTime.UtcNow,
            pendingExpiresAt: DateTime.UtcNow.AddHours(1));

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
        booking.Cancel(booking.CreatedAt);

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

        // Act
        booking.Cancel(booking.CreatedAt);

        // Assert
        Assert.Equal(booking.CreatedAt, booking.UpdatedAt);
    }

    [Fact]
    public void Confirm_WhenBookingIsPending_ShouldChangeStatusToConfirmedAndSetUpdatedAt()
    {
        var booking = CreateValidBooking();
        booking.Confirm(booking.CreatedAt);

        Assert.Equal(BookingStatus.Confirmed, booking.BookingStatus);
        Assert.Equal(booking.CreatedAt, booking.UpdatedAt);
    }

    [Fact]
    public void Confirm_WhenBookingIsCancelled_ShouldThrowInvalidOperationException()
    {
        var booking = CreateValidBooking();
        booking.Cancel(booking.CreatedAt);

        var action = () => booking.Confirm(booking.CreatedAt);

        Assert.Throws<InvalidOperationException>(action);
        Assert.Equal(BookingStatus.Cancelled, booking.BookingStatus);
    }

    [Fact]
    public void Confirm_WhenBookingIsAlreadyConfirmed_ShouldThrowInvalidOperationException()
    {
        var booking = CreateValidBooking();
        booking.Confirm(booking.CreatedAt);

        var action = () => booking.Confirm(booking.CreatedAt);

        Assert.Throws<InvalidOperationException>(action);
        Assert.Equal(BookingStatus.Confirmed, booking.BookingStatus);
    }

    [Fact]
    public void Constructor_WhenPendingExpirationIsNotUtc_ShouldThrowArgumentException()
    {
        var action = () => CreateValidBooking(
            pendingExpiresAt: DateTime.Now.AddHours(1));

        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Constructor_WhenPendingExpirationIsNotAfterCreation_ShouldThrowArgumentException()
    {
        var createdAt = new DateTime(2030, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        var action = () => CreateValidBooking(
            createdAt: createdAt,
            pendingExpiresAt: createdAt);

        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Expire_WhenPendingDeadlineHasPassed_ShouldCancelBooking()
    {
        var expiresAt = new DateTime(2030, 1, 1, 10, 15, 0, DateTimeKind.Utc);
        var booking = CreateValidBooking(
            createdAt: expiresAt.AddMinutes(-15),
            pendingExpiresAt: expiresAt);

        booking.Expire(expiresAt);

        Assert.Equal(BookingStatus.Cancelled, booking.BookingStatus);
        Assert.Equal(expiresAt, booking.UpdatedAt);
    }

    [Fact]
    public void Expire_WhenDeadlineHasNotPassed_ShouldThrowInvalidOperationException()
    {
        var expiresAt = new DateTime(2030, 1, 1, 10, 15, 0, DateTimeKind.Utc);
        var booking = CreateValidBooking(
            createdAt: expiresAt.AddMinutes(-15),
            pendingExpiresAt: expiresAt);

        var action = () => booking.Expire(expiresAt.AddTicks(-1));

        Assert.Throws<InvalidOperationException>(action);
        Assert.Equal(BookingStatus.Pending, booking.BookingStatus);
    }

    [Fact]
    public void Expire_WhenBookingIsNotPending_ShouldThrowInvalidOperationException()
    {
        var expiresAt = new DateTime(2030, 1, 1, 10, 15, 0, DateTimeKind.Utc);
        var booking = CreateValidBooking(
            createdAt: expiresAt.AddMinutes(-15),
            pendingExpiresAt: expiresAt);
        booking.Confirm(booking.CreatedAt);

        var action = () => booking.Expire(expiresAt);

        Assert.Throws<InvalidOperationException>(action);
        Assert.Equal(BookingStatus.Confirmed, booking.BookingStatus);
    }

    [Fact]
    public void Complete_WhenConfirmedStayHasEnded_ShouldMarkBookingCompleted()
    {
        var booking = CreateValidBooking();
        booking.Confirm(booking.CreatedAt);
        var completedAt = DateTime.SpecifyKind(booking.CheckOut.AddHours(1), DateTimeKind.Utc);

        booking.Complete(completedAt);

        Assert.Equal(BookingStatus.Completed, booking.BookingStatus);
        Assert.Equal(completedAt, booking.UpdatedAt);
    }

    [Fact]
    public void Complete_WhenCheckoutIsInFuture_ShouldThrowInvalidOperationException()
    {
        var booking = CreateValidBooking();
        booking.Confirm(booking.CreatedAt);
        var beforeCheckout = DateTime.SpecifyKind(booking.CheckOut.AddTicks(-1), DateTimeKind.Utc);

        var action = () => booking.Complete(beforeCheckout);

        Assert.Throws<InvalidOperationException>(action);
        Assert.Equal(BookingStatus.Confirmed, booking.BookingStatus);
    }

    [Fact]
    public void Cancel_WhenBookingIsCompleted_ShouldThrowInvalidOperationException()
    {
        var booking = CreateValidBooking();
        booking.Confirm(booking.CreatedAt);
        booking.Complete(DateTime.SpecifyKind(booking.CheckOut.AddHours(1), DateTimeKind.Utc));

        var action = () => booking.Cancel(
            DateTime.SpecifyKind(booking.CheckOut.AddHours(2), DateTimeKind.Utc));

        Assert.Throws<InvalidOperationException>(action);
        Assert.Equal(BookingStatus.Completed, booking.BookingStatus);
    }

    [Fact]
    public void CancelByCustomer_WhenStayHasStarted_ShouldThrowInvalidOperationException()
    {
        var booking = CreateValidBooking();
        var stayStartedAt = DateTime.SpecifyKind(booking.CheckIn, DateTimeKind.Utc);

        var action = () => booking.CancelByCustomer(stayStartedAt);

        Assert.Throws<InvalidOperationException>(action);
        Assert.Equal(BookingStatus.Pending, booking.BookingStatus);
    }

    [Fact]
    public void Modify_WhenBookingIsCancelled_ShouldThrowInvalidOperationException()
    {
        var booking = CreateValidBooking();
        var utcNow = DateTime.SpecifyKind(booking.CheckIn.AddDays(-1), DateTimeKind.Utc);
        booking.Cancel(utcNow);
        var stay = new BookingStay(20, booking.CheckIn, booking.CheckOut, 2, 0);
        var price = new BookingPrice(100m, 300m, 0, 0m, 300m);

        var action = () => booking.Modify(stay, price, null, utcNow);

        Assert.Throws<InvalidOperationException>(action);
        Assert.Equal(BookingStatus.Cancelled, booking.BookingStatus);
    }

    [Fact]
    public void AssignToInvoice_WhenInvoiceBelongsToUser_ShouldSetBothSidesOfRelationship()
    {
        var booking = CreateValidBooking();
        var invoice = new Invoice(booking.UserId, 1, booking.TotalPrice);

        booking.AssignToInvoice(invoice);

        Assert.Same(invoice, booking.Invoice);
        Assert.Contains(booking, invoice.Bookings);
    }

    [Fact]
    public void AssignToInvoice_WhenInvoiceBelongsToAnotherUser_ShouldThrowInvalidOperationException()
    {
        var booking = CreateValidBooking();
        var invoice = new Invoice(booking.UserId + 1, 1, booking.TotalPrice);

        var action = () => booking.AssignToInvoice(invoice);

        Assert.Throws<InvalidOperationException>(action);
        Assert.Null(booking.Invoice);
        Assert.DoesNotContain(booking, invoice.Bookings);
    }

    [Fact]
    public void AssignToInvoice_WhenBookingHasAnotherInvoice_ShouldThrowInvalidOperationException()
    {
        var booking = CreateValidBooking();
        var firstInvoice = new Invoice(booking.UserId, 1, booking.TotalPrice);
        var secondInvoice = new Invoice(booking.UserId, 1, booking.TotalPrice);
        booking.AssignToInvoice(firstInvoice);

        var action = () => booking.AssignToInvoice(secondInvoice);

        Assert.Throws<InvalidOperationException>(action);
        Assert.Same(firstInvoice, booking.Invoice);
        Assert.DoesNotContain(booking, secondInvoice.Bookings);
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
            new BookingStay(20, newCheckIn, newCheckOut, 3, 2),
            new BookingPrice(150m, 600m, 20, 120m, 480m),
            "Updated request",
            new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));

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

        var expectedUpdatedAt = new DateTime(
            2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        ModifyWithValidData(booking);

        // Assert
        Assert.Equal(expectedUpdatedAt, booking.UpdatedAt);
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
        Assert.ThrowsAny<ArgumentException>(action);
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
        Assert.ThrowsAny<ArgumentException>(action);
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
        Assert.ThrowsAny<ArgumentException>(action);
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
        Assert.ThrowsAny<ArgumentException>(action);
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
        Assert.ThrowsAny<ArgumentException>(action);
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
        Assert.ThrowsAny<ArgumentException>(action);
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
        Assert.ThrowsAny<ArgumentException>(action);
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
        Assert.ThrowsAny<ArgumentException>(action);
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
        string? specialRequests = "Test request",
        DateTime? createdAt = null,
        DateTime? pendingExpiresAt = null)
    {
        return BookingTestFactory.Create(
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
            specialRequests,
            createdAt ?? DateTime.UtcNow,
            pendingExpiresAt ?? DateTime.UtcNow.AddHours(1));
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
            new BookingStay(
                roomId,
                checkIn ?? new DateTime(2026, 11, 10),
                checkOut ?? new DateTime(2026, 11, 13),
                adults,
                children),
            new BookingPrice(
                pricePerNight,
                originalTotalPrice,
                discountPercentage,
                discountAmount,
                totalPrice),
            specialRequests,
            new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));
    }
}
