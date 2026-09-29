using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HotelBooking.Application.Bookings.Dtos;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Enums;
using HotelBooking.IntegrationTests.Infrastructure;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.IntegrationTests.Bookings;

public class BookingsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public BookingsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ModifyBooking_WhenAnonymous_ShouldReturnUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.PatchAsJsonAsync("/api/bookings/1", ValidRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ModifyBooking_WhenValid_ShouldUpdateGuestDetailsOnly()
    {
        var data = await CreateBookingDataAsync();
        using var client = CreateAuthenticatedClient(data.User);
        var request = new ModifyBookingRequestDto
        {
            Adults = 1,
            Children = 1,
            SpecialRequests = "Late arrival"
        };

        var response = await client.PatchAsJsonAsync($"/api/bookings/{data.Booking.BookingId}", request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var booking = await FindBookingAsync(data.Booking.BookingId);
        Assert.Equal(1, booking.Adults);
        Assert.Equal(1, booking.Children);
        Assert.Equal("Late arrival", booking.SpecialRequests);
        Assert.Equal(data.Room.RoomId, booking.RoomId);
        Assert.Equal(data.Booking.CheckIn, booking.CheckIn);
        Assert.Equal(data.Booking.CheckOut, booking.CheckOut);
        Assert.Equal(data.Booking.TotalPrice, booking.TotalPrice);
    }

    [Fact]
    public async Task ModifyBooking_WhenGuestCountExceedsCapacity_ShouldReturnBadRequest()
    {
        var data = await CreateBookingDataAsync();
        using var client = CreateAuthenticatedClient(data.User);
        var request = new ModifyBookingRequestDto { Adults = 3, Children = 0 };

        var response = await client.PatchAsJsonAsync($"/api/bookings/{data.Booking.BookingId}", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, -1)]
    public async Task ModifyBooking_WhenRequestIsInvalid_ShouldReturnBadRequest(int adults, int children)
    {
        var data = await CreateBookingDataAsync();
        using var client = CreateAuthenticatedClient(data.User);

        var response = await client.PatchAsJsonAsync(
            $"/api/bookings/{data.Booking.BookingId}",
            new ModifyBookingRequestDto { Adults = adults, Children = children });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ModifyBooking_WhenOwnedByAnotherCustomer_ShouldReturnNotFound()
    {
        var data = await CreateBookingDataAsync();
        var otherUser = await CreateUserAsync(Role.Customer);
        using var client = CreateAuthenticatedClient(otherUser);

        var response = await client.PatchAsJsonAsync(
            $"/api/bookings/{data.Booking.BookingId}",
            ValidRequest());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ModifyBooking_WhenCancelled_ShouldReturnConflict()
    {
        var data = await CreateBookingDataAsync(cancelled: true);
        using var client = CreateAuthenticatedClient(data.User);

        var response = await client.PatchAsJsonAsync(
            $"/api/bookings/{data.Booking.BookingId}",
            ValidRequest());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CancelBooking_WhenValid_ShouldPersistCancellation()
    {
        var data = await CreateBookingDataAsync();
        using var client = CreateAuthenticatedClient(data.User);

        var response = await client.DeleteAsync($"/api/bookings/{data.Booking.BookingId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(BookingStatus.Cancelled, (await FindBookingAsync(data.Booking.BookingId)).BookingStatus);
    }

    [Fact]
    public async Task CancelBooking_WhenPaid_ShouldPersistPartialRefund()
    {
        var data = await CreateBookingDataAsync(paid: true);
        using var client = CreateAuthenticatedClient(data.User);

        var response = await client.DeleteAsync($"/api/bookings/{data.Booking.BookingId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var booking = await FindBookingAsync(data.Booking.BookingId);
        Assert.Equal(BookingStatus.Cancelled, booking.BookingStatus);
        Assert.Equal(data.Booking.TotalPrice, booking.RefundedAmount);
        Assert.Equal(RefundStatus.Succeeded, booking.RefundStatus);
        Assert.Equal("re_test_partial", booking.ProviderRefundId);
    }

    [Fact]
    public async Task CancelBooking_WhenOwnedByAnotherCustomer_ShouldReturnNotFound()
    {
        var data = await CreateBookingDataAsync();
        var otherUser = await CreateUserAsync(Role.Customer);
        using var client = CreateAuthenticatedClient(otherUser);

        var response = await client.DeleteAsync($"/api/bookings/{data.Booking.BookingId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<BookingData> CreateBookingDataAsync(bool cancelled = false, bool paid = false)
    {
        var user = await CreateUserAsync(Role.Customer);
        var hotel = await CreateHotelAsync();
        var room = await CreateRoomAsync(hotel.HotelId);
        var checkIn = new DateTime(2030, 1, 10);
        var checkOut = new DateTime(2030, 1, 12);
        var booking = await CreateBookingAsync(user, hotel, room, checkIn, checkOut, cancelled, paid);
        return new BookingData(user, room, booking);
    }

    private async Task<User> CreateUserAsync(Role role)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var user = new User(
            "Test",
            "User",
            Unique("user"),
            $"{Guid.NewGuid():N}@test.com",
            hasher.HashPassword("Password123"),
            role);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private async Task<Hotel> CreateHotelAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var city = new City(Unique("City"), "Palestine", Unique("PO"));
        db.Cities.Add(city);
        await db.SaveChangesAsync();
        var hotel = new Hotel(
            Unique("Hotel"),
            "Owner",
            "Address",
            32.2,
            35.2,
            HotelType.Luxury,
            city.CityId,
            "Description",
            "History");
        db.Hotels.Add(hotel);
        await db.SaveChangesAsync();
        return hotel;
    }

    private async Task<Room> CreateRoomAsync(int hotelId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var room = new Room(Unique("Room"), RoomType.Double, 100m, 2, 2, hotelId, "Test room");
        db.Rooms.Add(room);
        await db.SaveChangesAsync();
        return room;
    }

    private async Task<Booking> CreateBookingAsync(
        User user,
        Hotel hotel,
        Room room,
        DateTime checkIn,
        DateTime checkOut,
        bool cancelled,
        bool paid)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var invoice = new Invoice(user.UserId, hotel.HotelId, 200m);
        var booking = BookingTestFactory.Create(
            user.UserId,
            room.RoomId,
            checkIn,
            checkOut,
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
        booking.AssignToInvoice(invoice);
        if (paid)
        {
            var payment = new Payment(booking.TotalPrice, DateTime.UtcNow);
            payment.AttachProviderPayment($"pi_{Guid.NewGuid():N}", "usd", null);
            payment.MarkAsPaid(DateTime.UtcNow);
            payment.AssignToInvoice(invoice);
            booking.Confirm(DateTime.UtcNow);
        }
        if (cancelled)
        {
            booking.Cancel(DateTime.UtcNow);
        }
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        return booking;
    }

    private async Task<Booking> FindBookingAsync(int bookingId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        return await db.Bookings.AsNoTracking().SingleAsync(booking => booking.BookingId == bookingId);
    }

    private HttpClient CreateAuthenticatedClient(User user)
    {
        using var scope = _factory.Services.CreateScope();
        var tokenGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenGenerator.GenerateToken(user));
        return client;
    }

    private static ModifyBookingRequestDto ValidRequest() => new()
    {
        Adults = 2,
        Children = 0
    };

    private static string Unique(string prefix) => $"{prefix}_{Guid.NewGuid():N}";

    private sealed record BookingData(User User, Room Room, Booking Booking);
}
