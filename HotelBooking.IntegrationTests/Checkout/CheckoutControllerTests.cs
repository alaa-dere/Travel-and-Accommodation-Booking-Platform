using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HotelBooking.Application.Bookings.Dtos;
using HotelBooking.Application.Checkout.Dtos;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Payments.Dtos;
using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Enums;
using HotelBooking.IntegrationTests.Infrastructure;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.IntegrationTests.Checkout;

public class CheckoutControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public CheckoutControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        GetEmailSender().Reset();
    }

    [Fact]
    public async Task CompleteCheckout_WithoutToken_ShouldReturnUnauthorized()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/checkout", ValidRequest());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CompleteCheckout_WithAdminToken_ShouldReturnForbidden()
    {
        var admin = await CreateUserAsync(Role.Admin);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.PostAsJsonAsync("/api/checkout", ValidRequest());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{invalid-json}")]
    public async Task CompleteCheckout_WithEmptyOrMalformedBody_ShouldReturnBadRequest(string body)
    {
        var user = await CreateUserAsync(Role.Customer);
        using var client = CreateAuthenticatedClient(user);
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/checkout", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CompleteCheckout_WithNullPayment_ShouldReturnBadRequest()
    {
        var user = await CreateUserAsync(Role.Customer);
        using var client = CreateAuthenticatedClient(user);
        using var content = new StringContent("{\"payment\":null}", Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/checkout", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CompleteCheckout_WithTooLongSpecialRequests_ShouldReturnBadRequest()
    {
        var user = await CreateUserAsync(Role.Customer);
        using var client = CreateAuthenticatedClient(user);
        var request = ValidRequest();
        request.SpecialRequests = new string('x', 1001);
        var response = await client.PostAsJsonAsync("/api/checkout", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CompleteCheckout_WhenCustomerNoLongerExists_ShouldReturnNotFound()
    {
        var user = await CreateUserAsync(Role.Customer);
        using var client = CreateAuthenticatedClient(user);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
            db.Users.Remove(await db.Users.SingleAsync(item => item.UserId == user.UserId));
            await db.SaveChangesAsync();
        }

        var response = await client.PostAsJsonAsync("/api/checkout", ValidRequest());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CompleteCheckout_WhenCartIsEmpty_ShouldReturnBadRequest()
    {
        var user = await CreateUserAsync(Role.Customer);
        using var client = CreateAuthenticatedClient(user);
        var response = await client.PostAsJsonAsync("/api/checkout", ValidRequest());
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CompleteCheckout_WhenPaymentSucceeds_ShouldCreateCompleteCheckoutAndClearCart()
    {
        var user = await CreateUserAsync(Role.Customer);
        var (hotel, room) = await CreateHotelAndRoomAsync(100m);
        var item = await CreateCartItemAsync(user, room,
            new DateTime(2030, 1, 10), new DateTime(2030, 1, 13));
        using var client = CreateAuthenticatedClient(user);
        var request = ValidRequest(true, "Late arrival");

        var response = await client.PostAsJsonAsync("/api/checkout", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<BookingCreationResultDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(1, result.BookingCount);
        Assert.Equal(1, result.InvoiceCount);
        var paymentResult = Assert.Single(result.Payments);
        Assert.Equal(PaymentStatus.Paid, paymentResult.Status);
        Assert.Equal(300m, paymentResult.Amount);
        var confirmation = Assert.Single(result.Confirmations);
        Assert.Equal(hotel.HotelId, confirmation.HotelId);
        Assert.Equal(hotel.Name, confirmation.HotelName);
        Assert.Equal(300m, confirmation.TotalAmount);
        Assert.Equal(PaymentStatus.Paid, confirmation.PaymentStatus);
        var roomConfirmation = Assert.Single(confirmation.Rooms);
        Assert.Equal(room.RoomId, roomConfirmation.RoomId);
        Assert.Equal(room.RoomNumber, roomConfirmation.RoomNumber);
        Assert.Equal(item.CheckIn, roomConfirmation.CheckIn);
        Assert.Equal(item.CheckOut, roomConfirmation.CheckOut);
        Assert.Equal(300m, roomConfirmation.TotalAmount);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var booking = await db.Bookings.AsNoTracking().SingleAsync(entry => entry.UserId == user.UserId);
        var invoice = await db.Invoices.AsNoTracking().SingleAsync(entry => entry.UserId == user.UserId);
        var payment = await db.Payments.AsNoTracking().SingleAsync(entry => entry.InvoiceId == invoice.InvoiceId);
        Assert.Equal(invoice.InvoiceId, confirmation.ConfirmationId);
        Assert.Equal(booking.BookingId, roomConfirmation.BookingId);
        Assert.Equal("Late arrival", booking.SpecialRequests);
        Assert.Equal(BookingStatus.Pending, booking.BookingStatus);
        Assert.Equal(300m, invoice.TotalAmount);
        Assert.Equal(PaymentStatus.Paid, payment.Status);
        Assert.NotNull(payment.ProcessedAt);
        Assert.False(await db.CartItems.AnyAsync(entry => entry.CartItemId == item.CartItemId));

        var email = Assert.Single(GetEmailSender().Messages);
        Assert.Equal(user.Email, email.RecipientEmail);
        Assert.Contains($"Invoice #{invoice.InvoiceId}", email.Subject);
        Assert.Contains(hotel.Name, email.Body);
        Assert.Contains(room.RoomNumber, email.Body);
    }

    [Fact]
    public async Task CompleteCheckout_WhenPaymentFails_ShouldCancelBookingAndKeepCart()
    {
        var user = await CreateUserAsync(Role.Customer);
        var (_, room) = await CreateHotelAndRoomAsync(100m);
        var item = await CreateCartItemAsync(user, room,
            new DateTime(2030, 1, 10), new DateTime(2030, 1, 12));
        using var client = CreateAuthenticatedClient(user);

        var response = await client.PostAsJsonAsync("/api/checkout", ValidRequest(false));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<BookingCreationResultDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(1, result.BookingCount);
        Assert.Equal(1, result.InvoiceCount);
        var paymentResult = Assert.Single(result.Payments);
        Assert.Equal(PaymentStatus.Failed, paymentResult.Status);
        Assert.Empty(result.Confirmations);
        Assert.Empty(GetEmailSender().Messages);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        Assert.Equal(BookingStatus.Cancelled,
            (await db.Bookings.AsNoTracking().SingleAsync(entry => entry.UserId == user.UserId)).BookingStatus);
        Assert.Equal(PaymentStatus.Failed,
            (await db.Payments.AsNoTracking().SingleAsync(entry => entry.Invoice!.UserId == user.UserId)).Status);
        Assert.True(await db.CartItems.AnyAsync(entry => entry.CartItemId == item.CartItemId));
    }

    [Fact]
    public async Task CompleteCheckout_WithTwoRoomsAtSameHotel_ShouldCreateOneInvoiceAndTwoBookings()
    {
        var user = await CreateUserAsync(Role.Customer);
        var hotel = await CreateHotelAsync();
        var firstRoom = await CreateRoomAsync(hotel.HotelId, 100m);
        var secondRoom = await CreateRoomAsync(hotel.HotelId, 150m);
        await CreateCartItemAsync(user, firstRoom, new DateTime(2030, 2, 1), new DateTime(2030, 2, 3));
        await CreateCartItemAsync(user, secondRoom, new DateTime(2030, 2, 5), new DateTime(2030, 2, 7));
        using var client = CreateAuthenticatedClient(user);

        var response = await client.PostAsJsonAsync("/api/checkout", ValidRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<BookingCreationResultDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(2, result.BookingCount);
        Assert.Equal(1, result.InvoiceCount);
        Assert.Single(result.Payments);
        Assert.Equal(500m, Assert.Single(result.Confirmations).TotalAmount);
        Assert.Equal(2, Assert.Single(result.Confirmations).Rooms.Count);
        Assert.Single(GetEmailSender().Messages);
    }

    [Fact]
    public async Task CompleteCheckout_WithRoomsAtDifferentHotels_ShouldCreateSeparateCheckoutGroups()
    {
        var user = await CreateUserAsync(Role.Customer);
        var (firstHotel, firstRoom) = await CreateHotelAndRoomAsync(100m);
        var (secondHotel, secondRoom) = await CreateHotelAndRoomAsync(200m);
        await CreateCartItemAsync(user, firstRoom, new DateTime(2030, 3, 1), new DateTime(2030, 3, 2));
        await CreateCartItemAsync(user, secondRoom, new DateTime(2030, 3, 4), new DateTime(2030, 3, 6));
        using var client = CreateAuthenticatedClient(user);

        var response = await client.PostAsJsonAsync("/api/checkout", ValidRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<BookingCreationResultDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(2, result.BookingCount);
        Assert.Equal(2, result.InvoiceCount);
        Assert.Equal(2, result.Payments.Count);
        Assert.Equal(2, result.Confirmations.Count);
        Assert.Contains(result.Confirmations, item => item.HotelId == firstHotel.HotelId && item.TotalAmount == 100m);
        Assert.Contains(result.Confirmations, item => item.HotelId == secondHotel.HotelId && item.TotalAmount == 400m);
        Assert.Equal(2, GetEmailSender().Messages.Count);
    }

    [Fact]
    public async Task CompleteCheckout_WithActivePromotion_ShouldPersistDiscountedPricing()
    {
        var user = await CreateUserAsync(Role.Customer);
        var (hotel, room) = await CreateHotelAndRoomAsync(100m);
        await CreatePromotionAsync(hotel.HotelId, 25);
        await CreateCartItemAsync(user, room, new DateTime(2030, 4, 1), new DateTime(2030, 4, 5));
        using var client = CreateAuthenticatedClient(user);

        var response = await client.PostAsJsonAsync("/api/checkout", ValidRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var booking = await db.Bookings.AsNoTracking().SingleAsync(entry => entry.UserId == user.UserId);
        Assert.Equal(400m, booking.OriginalTotalPrice);
        Assert.Equal(25, booking.DiscountPercentage);
        Assert.Equal(100m, booking.DiscountAmount);
        Assert.Equal(300m, booking.TotalPrice);
        Assert.Equal(300m, (await db.Invoices.AsNoTracking().SingleAsync(entry => entry.UserId == user.UserId)).TotalAmount);
    }

    [Fact]
    public async Task CompleteCheckout_WhenRoomBecameUnavailable_ShouldReturnConflictAndPreserveCart()
    {
        var user = await CreateUserAsync(Role.Customer);
        var otherUser = await CreateUserAsync(Role.Customer);
        var (hotel, room) = await CreateHotelAndRoomAsync(100m);
        var item = await CreateCartItemAsync(user, room,
            new DateTime(2030, 5, 10), new DateTime(2030, 5, 12));
        await CreateBookingAsync(otherUser, hotel, room,
            new DateTime(2030, 5, 11), new DateTime(2030, 5, 13));
        using var client = CreateAuthenticatedClient(user);

        var response = await client.PostAsJsonAsync("/api/checkout", ValidRequest());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertNoCheckoutRecordsAsync(user.UserId, item.CartItemId);
    }

    [Fact]
    public async Task CompleteCheckout_WhenLaterHotelFails_ShouldRollBackEarlierHotelChanges()
    {
        var user = await CreateUserAsync(Role.Customer);
        var otherUser = await CreateUserAsync(Role.Customer);
        var (firstHotel, firstRoom) = await CreateHotelAndRoomAsync(100m);
        var (secondHotel, secondRoom) = await CreateHotelAndRoomAsync(100m);
        var firstItem = await CreateCartItemAsync(user, firstRoom,
            new DateTime(2030, 6, 1), new DateTime(2030, 6, 3));
        var secondItem = await CreateCartItemAsync(user, secondRoom,
            new DateTime(2030, 6, 5), new DateTime(2030, 6, 7));
        await CreateBookingAsync(otherUser, secondHotel, secondRoom,
            new DateTime(2030, 6, 5), new DateTime(2030, 6, 7));
        using var client = CreateAuthenticatedClient(user);

        var response = await client.PostAsJsonAsync("/api/checkout", ValidRequest());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        Assert.False(await db.Bookings.AnyAsync(entry => entry.UserId == user.UserId));
        Assert.False(await db.Invoices.AnyAsync(entry => entry.UserId == user.UserId));
        Assert.False(await db.Payments.AnyAsync(entry => entry.Invoice!.UserId == user.UserId));
        Assert.True(await db.CartItems.AnyAsync(entry => entry.CartItemId == firstItem.CartItemId));
        Assert.True(await db.CartItems.AnyAsync(entry => entry.CartItemId == secondItem.CartItemId));
        Assert.Empty(GetEmailSender().Messages);
        Assert.NotEqual(firstHotel.HotelId, secondHotel.HotelId);
    }

    [Fact]
    public async Task CompleteCheckout_WhenEmailFails_ShouldStillCommitAndReturnSuccess()
    {
        var user = await CreateUserAsync(Role.Customer);
        var (_, room) = await CreateHotelAndRoomAsync(100m);
        await CreateCartItemAsync(user, room, new DateTime(2030, 7, 1), new DateTime(2030, 7, 3));
        GetEmailSender().ShouldThrow = true;
        using var client = CreateAuthenticatedClient(user);

        var response = await client.PostAsJsonAsync("/api/checkout", ValidRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<BookingCreationResultDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(1, result.BookingCount);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        Assert.True(await db.Bookings.AnyAsync(entry => entry.UserId == user.UserId));
        Assert.False(await db.CartItems.AnyAsync(entry => entry.UserId == user.UserId));
    }

    private async Task<User> CreateUserAsync(Role role)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var user = new User("Test", "User", Unique("user"), $"{Guid.NewGuid():N}@test.com",
            hasher.HashPassword("Password123"), role);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private async Task<(Hotel Hotel, Room Room)> CreateHotelAndRoomAsync(decimal price)
    {
        var hotel = await CreateHotelAsync();
        return (hotel, await CreateRoomAsync(hotel.HotelId, price));
    }

    private async Task<Hotel> CreateHotelAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var city = new City(Unique("City"), "Palestine", Unique("PO"));
        db.Cities.Add(city);
        await db.SaveChangesAsync();
        var hotel = new Hotel(Unique("Hotel"), "Owner", "Address", 32.2, 35.2,
            HotelType.Luxury, city.CityId, "Description", "History");
        db.Hotels.Add(hotel);
        await db.SaveChangesAsync();
        return hotel;
    }

    private async Task<Room> CreateRoomAsync(int hotelId, decimal price)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var room = new Room(Unique("Room"), RoomType.Double, price, 2, 1, hotelId, "Test room");
        db.Rooms.Add(room);
        await db.SaveChangesAsync();
        return room;
    }

    private async Task<CartItem> CreateCartItemAsync(
        User user, Room room, DateTime checkIn, DateTime checkOut)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var item = new CartItem(user.UserId, room.RoomId, checkIn, checkOut, 2, 0);
        db.CartItems.Add(item);
        await db.SaveChangesAsync();
        return item;
    }

    private async Task CreateBookingAsync(
        User user, Hotel hotel, Room room, DateTime checkIn, DateTime checkOut)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var total = (checkOut.Date - checkIn.Date).Days * room.PricePerNight;
        var invoice = new Invoice(user.UserId, hotel.HotelId, total);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
        var booking = new Booking(user.UserId, room.RoomId, checkIn, checkOut, 2, 0,
            room.PricePerNight, total, 0, 0m, total, null) { InvoiceId = invoice.InvoiceId };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
    }

    private async Task CreatePromotionAsync(int hotelId, int discount)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        db.Promotions.Add(new Promotion(hotelId, discount,
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1)));
        await db.SaveChangesAsync();
    }

    private async Task AssertNoCheckoutRecordsAsync(int userId, int cartItemId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        Assert.False(await db.Bookings.AnyAsync(entry => entry.UserId == userId));
        Assert.False(await db.Invoices.AnyAsync(entry => entry.UserId == userId));
        Assert.False(await db.Payments.AnyAsync(entry => entry.Invoice!.UserId == userId));
        Assert.True(await db.CartItems.AnyAsync(entry => entry.CartItemId == cartItemId));
        Assert.Empty(GetEmailSender().Messages);
    }

    private HttpClient CreateAuthenticatedClient(User user)
    {
        using var scope = _factory.Services.CreateScope();
        var generator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", generator.GenerateToken(user));
        return client;
    }

    private TestEmailSender GetEmailSender() => _factory.Services.GetRequiredService<TestEmailSender>();

    private static CompleteCheckoutRequestDto ValidRequest(bool shouldSucceed = true, string? specialRequests = null) => new()
    {
        SpecialRequests = specialRequests,
        Payment = new PaymentInformationDto { ShouldSucceed = shouldSucceed }
    };

    private static string Unique(string prefix) => $"{prefix}_{Guid.NewGuid():N}";
}
