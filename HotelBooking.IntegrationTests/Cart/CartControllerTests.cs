using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using HotelBooking.Application.Cart.Dtos;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.IntegrationTests.Infrastructure;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.IntegrationTests.Cart;

public class CartControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CartControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData(CartEndpoint.Add)]
    [InlineData(CartEndpoint.Get)]
    [InlineData(CartEndpoint.Remove)]
    public async Task CartEndpoint_WithoutToken_ShouldReturnUnauthorized(CartEndpoint endpoint)
    {
        using var client = _factory.CreateClient();
        using var response = await SendAsync(client, endpoint, 1, ValidRequest(1));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(CartEndpoint.Add)]
    [InlineData(CartEndpoint.Get)]
    [InlineData(CartEndpoint.Remove)]
    public async Task CartEndpoint_WithAdminToken_ShouldReturnForbidden(CartEndpoint endpoint)
    {
        var admin = await CreateUserAsync(Role.Admin);
        using var client = CreateAuthenticatedClient(admin);
        using var response = await SendAsync(client, endpoint, 1, ValidRequest(1));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AddItem_WithValidRequest_ShouldReturnNoContentAndPersistItem()
    {
        var user = await CreateUserAsync(Role.Customer);
        var (hotel, room) = await CreateHotelAndRoomAsync(2, 1);
        using var client = CreateAuthenticatedClient(user);
        var request = ValidRequest(room.RoomId);

        var response = await client.PostAsJsonAsync("/api/cart/items", request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var item = await db.CartItems.AsNoTracking().SingleAsync(entry => entry.UserId == user.UserId);
        Assert.Equal(room.RoomId, item.RoomId);
        Assert.Equal(request.CheckIn, item.CheckIn);
        Assert.Equal(request.CheckOut, item.CheckOut);
        Assert.Equal(request.Adults, item.Adults);
        Assert.Equal(request.Children, item.Children);
        Assert.Equal(hotel.HotelId, room.HotelId);
    }

    [Theory]
    [InlineData(RequestProblem.PastCheckIn)]
    [InlineData(RequestProblem.CheckOutBeforeCheckIn)]
    [InlineData(RequestProblem.CheckOutEqualsCheckIn)]
    [InlineData(RequestProblem.ZeroAdults)]
    [InlineData(RequestProblem.NegativeAdults)]
    [InlineData(RequestProblem.NegativeChildren)]
    public async Task AddItem_WithInvalidRequest_ShouldReturnBadRequest(RequestProblem problem)
    {
        var user = await CreateUserAsync(Role.Customer);
        var (_, room) = await CreateHotelAndRoomAsync(2, 1);
        var request = ValidRequest(room.RoomId);
        switch (problem)
        {
            case RequestProblem.PastCheckIn:
                request.CheckIn = DateTime.UtcNow.Date.AddDays(-2);
                request.CheckOut = DateTime.UtcNow.Date.AddDays(-1);
                break;
            case RequestProblem.CheckOutBeforeCheckIn:
                request.CheckOut = request.CheckIn.AddDays(-1);
                break;
            case RequestProblem.CheckOutEqualsCheckIn:
                request.CheckOut = request.CheckIn;
                break;
            case RequestProblem.ZeroAdults:
                request.Adults = 0;
                break;
            case RequestProblem.NegativeAdults:
                request.Adults = -1;
                break;
            case RequestProblem.NegativeChildren:
                request.Children = -1;
                break;
        }
        using var client = CreateAuthenticatedClient(user);

        var response = await client.PostAsJsonAsync("/api/cart/items", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{invalid-json}")]
    public async Task AddItem_WithEmptyOrMalformedBody_ShouldReturnBadRequest(string body)
    {
        var user = await CreateUserAsync(Role.Customer);
        using var client = CreateAuthenticatedClient(user);
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/cart/items", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddItem_WhenRoomDoesNotExist_ShouldReturnConflict()
    {
        var user = await CreateUserAsync(Role.Customer);
        using var client = CreateAuthenticatedClient(user);
        var response = await client.PostAsJsonAsync("/api/cart/items", ValidRequest(999999));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData(RoomProblem.Inactive)]
    [InlineData(RoomProblem.OperationallyUnavailable)]
    [InlineData(RoomProblem.InactiveHotel)]
    [InlineData(RoomProblem.AdultsCapacity)]
    [InlineData(RoomProblem.ChildrenCapacity)]
    public async Task AddItem_WhenRoomDoesNotMeetRequirements_ShouldReturnConflict(RoomProblem problem)
    {
        var user = await CreateUserAsync(Role.Customer);
        var (hotel, room) = await CreateHotelAndRoomAsync(2, 1);
        if (problem == RoomProblem.Inactive)
            await UpdateRoomAsync(room.RoomId, item => item.ChangeStatus(false));
        if (problem == RoomProblem.OperationallyUnavailable)
            await UpdateRoomAsync(room.RoomId, item => item.ChangeOperationalAvailability(false));
        if (problem == RoomProblem.InactiveHotel)
            await UpdateHotelAsync(hotel.HotelId, item => item.ChangeStatus(false));
        var request = ValidRequest(room.RoomId);
        if (problem == RoomProblem.AdultsCapacity) request.Adults = 3;
        if (problem == RoomProblem.ChildrenCapacity) request.Children = 2;
        using var client = CreateAuthenticatedClient(user);

        var response = await client.PostAsJsonAsync("/api/cart/items", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AddItem_WhenCapacityExactlyMatches_ShouldSucceed()
    {
        var user = await CreateUserAsync(Role.Customer);
        var (_, room) = await CreateHotelAndRoomAsync(2, 1);
        var request = ValidRequest(room.RoomId);
        request.Children = 1;
        using var client = CreateAuthenticatedClient(user);
        var response = await client.PostAsJsonAsync("/api/cart/items", request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Theory]
    [InlineData(false, "2030-01-11", "2030-01-12", HttpStatusCode.Conflict)]
    [InlineData(true, "2030-01-11", "2030-01-12", HttpStatusCode.NoContent)]
    [InlineData(false, "2030-01-08", "2030-01-10", HttpStatusCode.NoContent)]
    [InlineData(false, "2030-01-12", "2030-01-14", HttpStatusCode.NoContent)]
    public async Task AddItem_WithExistingBooking_ShouldApplyAvailabilityRules(
        bool cancelled, string checkIn, string checkOut, HttpStatusCode expectedStatus)
    {
        var user = await CreateUserAsync(Role.Customer);
        var otherUser = await CreateUserAsync(Role.Customer);
        var (hotel, room) = await CreateHotelAndRoomAsync(2, 1);
        await CreateBookingAsync(otherUser, hotel, room,
            new DateTime(2030, 1, 10), new DateTime(2030, 1, 12), cancelled);
        var request = ValidRequest(room.RoomId);
        request.CheckIn = DateTime.Parse(checkIn);
        request.CheckOut = DateTime.Parse(checkOut);
        using var client = CreateAuthenticatedClient(user);

        var response = await client.PostAsJsonAsync("/api/cart/items", request);

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Fact]
    public async Task GetCart_WhenEmpty_ShouldReturnEmptyArray()
    {
        var user = await CreateUserAsync(Role.Customer);
        using var client = CreateAuthenticatedClient(user);
        var response = await client.GetAsync("/api/cart");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<CartItemResponseDto>>();
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCart_ShouldReturnOnlyCurrentUsersItemsWithMappedFieldsInCreationOrder()
    {
        var user = await CreateUserAsync(Role.Customer);
        var otherUser = await CreateUserAsync(Role.Customer);
        var (firstHotel, firstRoom) = await CreateHotelAndRoomAsync(2, 1);
        var (secondHotel, secondRoom) = await CreateHotelAndRoomAsync(3, 2);
        var (_, otherRoom) = await CreateHotelAndRoomAsync(2, 0);
        var first = await CreateCartItemAsync(user, firstRoom, new DateTime(2030, 2, 1), new DateTime(2030, 2, 3), 2, 1);
        var second = await CreateCartItemAsync(user, secondRoom, new DateTime(2030, 3, 1), new DateTime(2030, 3, 4), 3, 2);
        await CreateCartItemAsync(otherUser, otherRoom, new DateTime(2030, 4, 1), new DateTime(2030, 4, 2), 1, 0);
        using var client = CreateAuthenticatedClient(user);

        var response = await client.GetAsync("/api/cart");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<CartItemResponseDto>>();
        Assert.NotNull(result);
        Assert.Collection(result,
            item => AssertCartItem(item, first, firstRoom, firstHotel),
            item => AssertCartItem(item, second, secondRoom, secondHotel));
    }

    [Fact]
    public async Task RemoveItem_WhenOwnedByCustomer_ShouldReturnNoContentAndDeleteItem()
    {
        var user = await CreateUserAsync(Role.Customer);
        var (_, room) = await CreateHotelAndRoomAsync(2, 1);
        var item = await CreateCartItemAsync(user, room,
            new DateTime(2030, 1, 10), new DateTime(2030, 1, 12), 2, 0);
        using var client = CreateAuthenticatedClient(user);

        var response = await client.DeleteAsync($"/api/cart/items/{item.CartItemId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        Assert.False(await db.CartItems.AnyAsync(entry => entry.CartItemId == item.CartItemId));
    }

    [Fact]
    public async Task RemoveItem_WhenItemDoesNotExist_ShouldReturnNotFound()
    {
        var user = await CreateUserAsync(Role.Customer);
        using var client = CreateAuthenticatedClient(user);
        var response = await client.DeleteAsync("/api/cart/items/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RemoveItem_WhenOwnedByDifferentCustomer_ShouldReturnNotFoundAndPreserveItem()
    {
        var owner = await CreateUserAsync(Role.Customer);
        var otherUser = await CreateUserAsync(Role.Customer);
        var (_, room) = await CreateHotelAndRoomAsync(2, 1);
        var item = await CreateCartItemAsync(owner, room,
            new DateTime(2030, 1, 10), new DateTime(2030, 1, 12), 2, 0);
        using var client = CreateAuthenticatedClient(otherUser);

        var response = await client.DeleteAsync($"/api/cart/items/{item.CartItemId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        Assert.True(await db.CartItems.AnyAsync(entry => entry.CartItemId == item.CartItemId));
    }

    [Fact]
    public async Task RemoveItem_WhenIdIsZero_ShouldReturnNotFound()
    {
        var user = await CreateUserAsync(Role.Customer);
        using var client = CreateAuthenticatedClient(user);
        var response = await client.DeleteAsync("/api/cart/items/0");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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

    private async Task<(Hotel Hotel, Room Room)> CreateHotelAndRoomAsync(int adults, int children)
    {
        var hotel = await CreateHotelAsync();
        var room = await CreateRoomAsync(hotel.HotelId, adults, children);
        return (hotel, room);
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

    private async Task<Room> CreateRoomAsync(int hotelId, int adults, int children)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var room = new Room(Unique("Room"), RoomType.Double, 100m, adults, children, hotelId, "Test room");
        db.Rooms.Add(room);
        await db.SaveChangesAsync();
        return room;
    }

    private async Task<CartItem> CreateCartItemAsync(
        User user, Room room, DateTime checkIn, DateTime checkOut, int adults, int children)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var item = new CartItem(user.UserId, room.RoomId, checkIn, checkOut, adults, children);
        db.CartItems.Add(item);
        await db.SaveChangesAsync();
        return item;
    }

    private async Task CreateBookingAsync(
        User user, Hotel hotel, Room room, DateTime checkIn, DateTime checkOut, bool cancelled)
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

    private async Task UpdateHotelAsync(int hotelId, Action<Hotel> update)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var hotel = await db.Hotels.FindAsync(hotelId);
        Assert.NotNull(hotel);
        update(hotel);
        await db.SaveChangesAsync();
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

    private static AddCartItemRequestDto ValidRequest(int roomId) => new()
    {
        RoomId = roomId,
        CheckIn = new DateTime(2030, 1, 10),
        CheckOut = new DateTime(2030, 1, 12),
        Adults = 2,
        Children = 0
    };

    private static void AssertCartItem(CartItemResponseDto actual, CartItem expected, Room room, Hotel hotel)
    {
        Assert.Equal(expected.CartItemId, actual.CartItemId);
        Assert.Equal(room.RoomId, actual.RoomId);
        Assert.Equal(room.RoomNumber, actual.RoomNumber);
        Assert.Equal(hotel.HotelId, actual.HotelId);
        Assert.Equal(hotel.Name, actual.HotelName);
        Assert.Equal(expected.CheckIn, actual.CheckIn);
        Assert.Equal(expected.CheckOut, actual.CheckOut);
        Assert.Equal(expected.Adults, actual.Adults);
        Assert.Equal(expected.Children, actual.Children);
    }

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client, CartEndpoint endpoint, int id, AddCartItemRequestDto request) => endpoint switch
    {
        CartEndpoint.Add => await client.PostAsJsonAsync("/api/cart/items", request),
        CartEndpoint.Get => await client.GetAsync("/api/cart"),
        _ => await client.DeleteAsync($"/api/cart/items/{id}")
    };

    private static string Unique(string prefix) => $"{prefix}_{Guid.NewGuid():N}";

    public enum CartEndpoint { Add, Get, Remove }
    public enum RequestProblem { PastCheckIn, CheckOutBeforeCheckIn, CheckOutEqualsCheckIn, ZeroAdults, NegativeAdults, NegativeChildren }
    public enum RoomProblem { Inactive, OperationallyUnavailable, InactiveHotel, AdultsCapacity, ChildrenCapacity }
}
