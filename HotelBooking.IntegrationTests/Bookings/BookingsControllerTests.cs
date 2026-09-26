using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using HotelBooking.Application.Bookings.Dtos;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
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

    [Theory]
    [InlineData(HttpMethodName.Put)]
    [InlineData(HttpMethodName.Delete)]
    public async Task BookingEndpoint_WithoutToken_ShouldReturnUnauthorized(HttpMethodName method)
    {
        using var client = _factory.CreateClient();
        using var response = await SendAsync(client, method, 1, ValidRequest(1));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(HttpMethodName.Put)]
    [InlineData(HttpMethodName.Delete)]
    public async Task BookingEndpoint_WithAdminToken_ShouldReturnForbidden(HttpMethodName method)
    {
        var admin = await CreateUserAsync(Role.Admin);
        using var client = CreateAuthenticatedClient(admin);
        using var response = await SendAsync(client, method, 1, ValidRequest(1));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ModifyBooking_WithValidRequest_ShouldPersistAllChangesAndRecalculateTotals()
    {
        var data = await CreateBookingDataAsync();
        var replacementRoom = await CreateRoomAsync(data.Hotel.HotelId, 4, 3, 150m);
        using var client = CreateAuthenticatedClient(data.User);
        var request = new ModifyBookingRequestDto
        {
            RoomId = replacementRoom.RoomId,
            CheckIn = new DateTime(2030, 2, 10),
            CheckOut = new DateTime(2030, 2, 13),
            Adults = 3,
            Children = 2,
            SpecialRequests = "Late arrival"
        };

        var response = await client.PutAsJsonAsync($"/api/bookings/{data.Booking.BookingId}", request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var booking = await db.Bookings.AsNoTracking().SingleAsync(item => item.BookingId == data.Booking.BookingId);
        var invoice = await db.Invoices.AsNoTracking().SingleAsync(item => item.InvoiceId == data.Invoice.InvoiceId);
        Assert.Equal(replacementRoom.RoomId, booking.RoomId);
        Assert.Equal(request.CheckIn, booking.CheckIn);
        Assert.Equal(request.CheckOut, booking.CheckOut);
        Assert.Equal(3, booking.Adults);
        Assert.Equal(2, booking.Children);
        Assert.Equal("Late arrival", booking.SpecialRequests);
        Assert.Equal(150m, booking.PricePerNight);
        Assert.Equal(450m, booking.OriginalTotalPrice);
        Assert.Equal(450m, booking.TotalPrice);
        Assert.Equal(450m, invoice.TotalAmount);
    }

    [Fact]
    public async Task ModifyBooking_WhenRoomAndDatesAreUnchanged_ShouldKeepPriceAndUpdateOtherFields()
    {
        var data = await CreateBookingDataAsync();
        using var client = CreateAuthenticatedClient(data.User);
        var request = ValidRequest(data.Room.RoomId);
        request.Adults = 1;
        request.Children = 1;
        request.SpecialRequests = "Quiet room";

        var response = await client.PutAsJsonAsync($"/api/bookings/{data.Booking.BookingId}", request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var booking = await FindBookingAsync(data.Booking.BookingId);
        Assert.Equal(100m, booking.PricePerNight);
        Assert.Equal(200m, booking.TotalPrice);
        Assert.Equal(1, booking.Adults);
        Assert.Equal(1, booking.Children);
        Assert.Equal("Quiet room", booking.SpecialRequests);
    }

    [Theory]
    [InlineData(0, 2, 0, "2030-01-10", "2030-01-12", null)]
    [InlineData(1, 0, 0, "2030-01-10", "2030-01-12", null)]
    [InlineData(1, -1, 0, "2030-01-10", "2030-01-12", null)]
    [InlineData(1, 2, -1, "2030-01-10", "2030-01-12", null)]
    [InlineData(1, 2, 0, "2030-01-12", "2030-01-10", null)]
    [InlineData(1, 2, 0, "2030-01-10", "2030-01-10", null)]
    [InlineData(1, 2, 0, "2030-01-10", "2030-01-12", "too-long")]
    public async Task ModifyBooking_WithInvalidRequest_ShouldReturnBadRequest(
        int roomSelector, int adults, int children, string checkIn, string checkOut, string? specialRequests)
    {
        var data = await CreateBookingDataAsync();
        using var client = CreateAuthenticatedClient(data.User);
        var request = new ModifyBookingRequestDto
        {
            RoomId = roomSelector == 0 ? 0 : data.Room.RoomId,
            CheckIn = DateTime.Parse(checkIn),
            CheckOut = DateTime.Parse(checkOut),
            Adults = adults,
            Children = children,
            SpecialRequests = specialRequests == "too-long" ? new string('x', 1001) : specialRequests
        };

        var response = await client.PutAsJsonAsync($"/api/bookings/{data.Booking.BookingId}", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{invalid-json}")]
    public async Task ModifyBooking_WithEmptyOrMalformedBody_ShouldReturnBadRequest(string body)
    {
        var user = await CreateUserAsync(Role.Customer);
        using var client = CreateAuthenticatedClient(user);
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await client.PutAsync("/api/bookings/1", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ModifyBooking_WithMissingCheckIn_ShouldReturnBadRequest()
    {
        var data = await CreateBookingDataAsync();
        using var client = CreateAuthenticatedClient(data.User);
        var request = ValidRequest(data.Room.RoomId);
        request.CheckIn = default;

        var response = await client.PutAsJsonAsync($"/api/bookings/{data.Booking.BookingId}", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ModifyBooking_WhenBookingDoesNotExist_ShouldReturnNotFound()
    {
        var user = await CreateUserAsync(Role.Customer);
        var room = await CreateHotelAndRoomAsync();
        using var client = CreateAuthenticatedClient(user);
        var response = await client.PutAsJsonAsync("/api/bookings/999999", ValidRequest(room.RoomId));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ModifyBooking_WhenOwnedByDifferentCustomer_ShouldReturnNotFound()
    {
        var data = await CreateBookingDataAsync();
        var otherUser = await CreateUserAsync(Role.Customer);
        using var client = CreateAuthenticatedClient(otherUser);
        var response = await client.PutAsJsonAsync(
            $"/api/bookings/{data.Booking.BookingId}", ValidRequest(data.Room.RoomId));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ModifyBooking_WhenStayHasStarted_ShouldReturnConflict()
    {
        var data = await CreateBookingDataAsync(DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(1));
        using var client = CreateAuthenticatedClient(data.User);
        var request = ValidRequest(data.Room.RoomId);
        request.CheckIn = data.Booking.CheckIn;
        request.CheckOut = data.Booking.CheckOut;
        var response = await client.PutAsJsonAsync($"/api/bookings/{data.Booking.BookingId}", request);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ModifyBooking_WhenBookingIsCancelled_ShouldReturnConflict()
    {
        var data = await CreateBookingDataAsync(cancelled: true);
        using var client = CreateAuthenticatedClient(data.User);

        var response = await client.PutAsJsonAsync(
            $"/api/bookings/{data.Booking.BookingId}", ValidRequest(data.Room.RoomId));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData(RoomProblem.NotFound, HttpStatusCode.NotFound)]
    [InlineData(RoomProblem.Inactive, HttpStatusCode.Conflict)]
    [InlineData(RoomProblem.OperationallyUnavailable, HttpStatusCode.Conflict)]
    [InlineData(RoomProblem.AdultsCapacity, HttpStatusCode.BadRequest)]
    [InlineData(RoomProblem.ChildrenCapacity, HttpStatusCode.BadRequest)]
    [InlineData(RoomProblem.DifferentHotel, HttpStatusCode.BadRequest)]
    public async Task ModifyBooking_WithInvalidRequestedRoom_ShouldReturnExpectedStatus(
        RoomProblem problem, HttpStatusCode expectedStatus)
    {
        var data = await CreateBookingDataAsync();
        var request = ValidRequest(data.Room.RoomId);
        if (problem == RoomProblem.NotFound)
        {
            request.RoomId = 999999;
        }
        else
        {
            var hotel = problem == RoomProblem.DifferentHotel ? await CreateHotelAsync() : data.Hotel;
            var room = await CreateRoomAsync(hotel.HotelId, 2, 1, 100m);
            if (problem == RoomProblem.Inactive)
                await UpdateRoomAsync(room.RoomId, item => item.ChangeStatus(false));
            if (problem == RoomProblem.OperationallyUnavailable)
                await UpdateRoomAsync(room.RoomId, item => item.ChangeOperationalAvailability(false));
            request.RoomId = room.RoomId;
            if (problem == RoomProblem.AdultsCapacity) request.Adults = 3;
            if (problem == RoomProblem.ChildrenCapacity) request.Children = 2;
        }

        using var client = CreateAuthenticatedClient(data.User);
        var response = await client.PutAsJsonAsync($"/api/bookings/{data.Booking.BookingId}", request);
        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Fact]
    public async Task ModifyBooking_WhenRequestedRoomHasOverlappingBooking_ShouldReturnConflict()
    {
        var data = await CreateBookingDataAsync();
        var occupiedRoom = await CreateRoomAsync(data.Hotel.HotelId, 2, 1, 100m);
        var otherUser = await CreateUserAsync(Role.Customer);
        await CreateBookingAsync(otherUser, data.Hotel, occupiedRoom,
            new DateTime(2030, 1, 11), new DateTime(2030, 1, 13));
        var request = ValidRequest(occupiedRoom.RoomId);
        using var client = CreateAuthenticatedClient(data.User);

        var response = await client.PutAsJsonAsync($"/api/bookings/{data.Booking.BookingId}", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("2030-01-08", "2030-01-10")]
    [InlineData("2030-01-12", "2030-01-14")]
    public async Task ModifyBooking_WhenRequestedDatesTouchAnotherBookingBoundary_ShouldSucceed(
        string checkIn, string checkOut)
    {
        var data = await CreateBookingDataAsync();
        var requestedRoom = await CreateRoomAsync(data.Hotel.HotelId, 2, 1, 100m);
        var otherUser = await CreateUserAsync(Role.Customer);
        await CreateBookingAsync(otherUser, data.Hotel, requestedRoom,
            new DateTime(2030, 1, 10), new DateTime(2030, 1, 12));
        var request = ValidRequest(requestedRoom.RoomId);
        request.CheckIn = DateTime.Parse(checkIn);
        request.CheckOut = DateTime.Parse(checkOut);
        using var client = CreateAuthenticatedClient(data.User);

        var response = await client.PutAsJsonAsync($"/api/bookings/{data.Booking.BookingId}", request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task CancelBooking_WhenValid_ShouldReturnNoContentAndPersistCancellation()
    {
        var data = await CreateBookingDataAsync();
        using var client = CreateAuthenticatedClient(data.User);
        var response = await client.DeleteAsync($"/api/bookings/{data.Booking.BookingId}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var booking = await FindBookingAsync(data.Booking.BookingId);
        Assert.Equal(BookingStatus.Cancelled, booking.BookingStatus);
        Assert.NotNull(booking.UpdatedAt);
    }

    [Fact]
    public async Task CancelBooking_WhenBookingIdIsInvalid_ShouldReturnBadRequest()
    {
        var user = await CreateUserAsync(Role.Customer);
        using var client = CreateAuthenticatedClient(user);
        var response = await client.DeleteAsync("/api/bookings/0");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CancelBooking_WhenBookingDoesNotExist_ShouldReturnNotFound()
    {
        var user = await CreateUserAsync(Role.Customer);
        using var client = CreateAuthenticatedClient(user);
        var response = await client.DeleteAsync("/api/bookings/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CancelBooking_WhenOwnedByDifferentCustomer_ShouldReturnNotFound()
    {
        var data = await CreateBookingDataAsync();
        var otherUser = await CreateUserAsync(Role.Customer);
        using var client = CreateAuthenticatedClient(otherUser);
        var response = await client.DeleteAsync($"/api/bookings/{data.Booking.BookingId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CancelBooking_WhenAlreadyCancelled_ShouldReturnConflict()
    {
        var data = await CreateBookingDataAsync(cancelled: true);
        using var client = CreateAuthenticatedClient(data.User);
        var response = await client.DeleteAsync($"/api/bookings/{data.Booking.BookingId}");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CancelBooking_WhenStayHasStarted_ShouldReturnConflict()
    {
        var data = await CreateBookingDataAsync(DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(1));
        using var client = CreateAuthenticatedClient(data.User);
        var response = await client.DeleteAsync($"/api/bookings/{data.Booking.BookingId}");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private async Task<BookingData> CreateBookingDataAsync(
        DateTime? checkIn = null, DateTime? checkOut = null, bool cancelled = false)
    {
        var user = await CreateUserAsync(Role.Customer);
        var hotel = await CreateHotelAsync();
        var room = await CreateRoomAsync(hotel.HotelId, 2, 1, 100m);
        var (booking, invoice) = await CreateBookingAsync(
            user, hotel, room,
            checkIn ?? new DateTime(2030, 1, 10),
            checkOut ?? new DateTime(2030, 1, 12),
            cancelled);
        return new BookingData(user, hotel, room, booking, invoice);
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

    private async Task<Room> CreateHotelAndRoomAsync()
    {
        var hotel = await CreateHotelAsync();
        return await CreateRoomAsync(hotel.HotelId, 2, 1, 100m);
    }

    private async Task<Room> CreateRoomAsync(int hotelId, int adults, int children, decimal price)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var room = new Room(Unique("Room"), RoomType.Double, price, adults, children, hotelId, "Test room");
        db.Rooms.Add(room);
        await db.SaveChangesAsync();
        return room;
    }

    private async Task<(Booking Booking, Invoice Invoice)> CreateBookingAsync(
        User user, Hotel hotel, Room room, DateTime checkIn, DateTime checkOut, bool cancelled = false)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var total = (checkOut.Date - checkIn.Date).Days * room.PricePerNight;
        var invoice = new Invoice(user.UserId, hotel.HotelId, total);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
        var booking = new Booking(user.UserId, room.RoomId, checkIn, checkOut, 2, 0,
            room.PricePerNight, total, 0, 0m, total, null) { InvoiceId = invoice.InvoiceId };
        if (cancelled) booking.Cancel();
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        return (booking, invoice);
    }

    private async Task UpdateRoomAsync(int roomId, Action<Room> update)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var room = await db.Rooms.FindAsync(roomId);
        Assert.NotNull(room);
        update(room);
        await db.SaveChangesAsync();
    }

    private async Task<Booking> FindBookingAsync(int bookingId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        return await db.Bookings.AsNoTracking().SingleAsync(item => item.BookingId == bookingId);
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

    private static ModifyBookingRequestDto ValidRequest(int roomId) => new()
    {
        RoomId = roomId,
        CheckIn = new DateTime(2030, 1, 10),
        CheckOut = new DateTime(2030, 1, 12),
        Adults = 2,
        Children = 0
    };

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client, HttpMethodName method, int bookingId, ModifyBookingRequestDto request)
    {
        return method == HttpMethodName.Put
            ? await client.PutAsJsonAsync($"/api/bookings/{bookingId}", request)
            : await client.DeleteAsync($"/api/bookings/{bookingId}");
    }

    private static string Unique(string prefix) => $"{prefix}_{Guid.NewGuid():N}";

    public enum HttpMethodName { Put, Delete }
    public enum RoomProblem { NotFound, Inactive, OperationallyUnavailable, AdultsCapacity, ChildrenCapacity, DifferentHotel }
    private sealed record BookingData(User User, Hotel Hotel, Room Room, Booking Booking, Invoice Invoice);
}
