using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HotelBooking.Application.FeatureDeals.Dtos;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.IntegrationTests.Infrastructure;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.IntegrationTests.FeaturedDeals;

public class FeaturedDealsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public FeaturedDealsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetFeaturedDeals_WithoutToken_ShouldReturnUnauthorized()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/FeaturedDeals");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetFeaturedDeals_WithAdminToken_ShouldReturnForbidden()
    {
        var admin = await CreateUserAsync(Role.Admin);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.GetAsync("/api/FeaturedDeals");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetFeaturedDeals_WhenNoEligibleHotels_ShouldReturnEmptyArray()
    {
        using var isolatedFactory = new CustomWebApplicationFactory();
        var customer = await CreateUserAsync(Role.Customer, isolatedFactory);
        using var client = CreateAuthenticatedClient(customer, isolatedFactory);
        var response = await client.GetAsync("/api/FeaturedDeals");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<FeaturedDealResponseDto>>();
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetFeaturedDeals_ShouldMapHotelPricingPromotionImageAndRating()
    {
        var customer = await CreateUserAsync(Role.Customer);
        var hotel = await CreateHotelAsync("Mapped City");
        var expensiveRoom = await CreateRoomAsync(hotel.HotelId, 250m);
        var cheapestRoom = await CreateRoomAsync(hotel.HotelId, 120m);
        var inactiveCheaperRoom = await CreateRoomAsync(hotel.HotelId, 50m);
        await UpdateRoomAsync(inactiveCheaperRoom.RoomId, room => room.ChangeStatus(false));
        await CreatePromotionAsync(hotel.HotelId, 10);
        await CreatePromotionAsync(hotel.HotelId, 25);
        await CreateHotelImagesAsync(hotel.HotelId, ("second.jpg", 2), ("first.jpg", 1));
        var firstBooking = await CreateBookingAsync(customer, hotel, expensiveRoom, false, DateTime.UtcNow.AddDays(-2));
        var secondBooking = await CreateBookingAsync(customer, hotel, cheapestRoom, false, DateTime.UtcNow.AddDays(-1));
        await CreateReviewAsync(firstBooking.BookingId, 4);
        await CreateReviewAsync(secondBooking.BookingId, 2);
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync("/api/FeaturedDeals");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<FeaturedDealResponseDto>>();
        Assert.NotNull(result);
        var deal = Assert.Single(result.Where(item => item.HotelId == hotel.HotelId));
        Assert.Equal(hotel.Name, deal.HotelName);
        Assert.Equal("Mapped City", deal.City);
        Assert.Equal(hotel.Address, deal.Address);
        Assert.Equal("first.jpg", deal.ThumbnailUrl);
        Assert.Equal(3m, deal.Rating);
        Assert.Equal(120m, deal.OriginalPrice);
        Assert.Equal(25, deal.DiscountPercentage);
        Assert.Equal(90m, deal.DiscountedPrice);
    }

    [Fact]
    public async Task GetFeaturedDeals_WithoutImagesOrReviews_ShouldMapNullValues()
    {
        var customer = await CreateUserAsync(Role.Customer);
        var dealData = await CreateEligibleDealAsync();
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync("/api/FeaturedDeals");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<FeaturedDealResponseDto>>();
        Assert.NotNull(result);
        var deal = Assert.Single(result.Where(item => item.HotelId == dealData.Hotel.HotelId));
        Assert.Null(deal.ThumbnailUrl);
        Assert.Null(deal.Rating);
    }

    [Theory]
    [InlineData(IneligibleReason.InactiveHotel)]
    [InlineData(IneligibleReason.NoRooms)]
    [InlineData(IneligibleReason.InactiveRoom)]
    [InlineData(IneligibleReason.OperationallyUnavailableRoom)]
    [InlineData(IneligibleReason.NoPromotion)]
    [InlineData(IneligibleReason.InactivePromotion)]
    [InlineData(IneligibleReason.ExpiredPromotion)]
    [InlineData(IneligibleReason.FuturePromotion)]
    public async Task GetFeaturedDeals_ShouldExcludeIneligibleHotel(IneligibleReason reason)
    {
        var customer = await CreateUserAsync(Role.Customer);
        var hotel = await CreateHotelAsync();
        Room? room = null;
        if (reason != IneligibleReason.NoRooms)
        {
            room = await CreateRoomAsync(hotel.HotelId, 100m);
        }

        if (reason != IneligibleReason.NoPromotion)
        {
            var now = DateTime.UtcNow;
            var start = reason == IneligibleReason.FuturePromotion ? now.AddDays(1) : now.AddDays(-2);
            var end = reason == IneligibleReason.ExpiredPromotion ? now.AddDays(-1) : now.AddDays(2);
            if (reason == IneligibleReason.ExpiredPromotion) start = now.AddDays(-3);
            var promotion = await CreatePromotionAsync(hotel.HotelId, 20, start, end);
            if (reason == IneligibleReason.InactivePromotion)
                await UpdatePromotionAsync(promotion.PromotionId, item => item.Deactivate());
        }

        if (reason == IneligibleReason.InactiveHotel)
            await UpdateHotelAsync(hotel.HotelId, item => item.ChangeStatus(false));
        if (reason == IneligibleReason.InactiveRoom)
            await UpdateRoomAsync(room!.RoomId, item => item.ChangeStatus(false));
        if (reason == IneligibleReason.OperationallyUnavailableRoom)
            await UpdateRoomAsync(room!.RoomId, item => item.ChangeOperationalAvailability(false));
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync("/api/FeaturedDeals");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<FeaturedDealResponseDto>>();
        Assert.NotNull(result);
        Assert.DoesNotContain(result, item => item.HotelId == hotel.HotelId);
    }

    [Fact]
    public async Task GetFeaturedDeals_ShouldOrderByRecentNonCancelledBookingCountDescendingAndTakeFive()
    {
        var customer = await CreateUserAsync(Role.Customer);
        var expectedHotelIds = new List<int>();
        for (var bookingCount = 0; bookingCount < 6; bookingCount++)
        {
            var deal = await CreateEligibleDealAsync();
            for (var bookingIndex = 0; bookingIndex < bookingCount + 10; bookingIndex++)
            {
                await CreateBookingAsync(customer, deal.Hotel, deal.Room, false,
                    DateTime.UtcNow.AddDays(-bookingIndex - 1));
            }
            expectedHotelIds.Add(deal.Hotel.HotelId);
        }
        expectedHotelIds.Reverse();
        expectedHotelIds = expectedHotelIds.Take(5).ToList();
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync("/api/FeaturedDeals");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<FeaturedDealResponseDto>>();
        Assert.NotNull(result);
        Assert.Equal(5, result.Count);
        Assert.Equal(expectedHotelIds, result.Select(item => item.HotelId));
    }

    [Fact]
    public async Task GetFeaturedDeals_ShouldIgnoreCancelledAndOlderThanThirtyDayBookingsForRanking()
    {
        var customer = await CreateUserAsync(Role.Customer);
        var popular = await CreateEligibleDealAsync();
        var ignored = await CreateEligibleDealAsync();
        await CreateBookingAsync(customer, popular.Hotel, popular.Room, false, DateTime.UtcNow.AddDays(-1));
        await CreateBookingAsync(customer, ignored.Hotel, ignored.Room, true, DateTime.UtcNow.AddDays(-1));
        await CreateBookingAsync(customer, ignored.Hotel, ignored.Room, false, DateTime.UtcNow.AddDays(-31));
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync("/api/FeaturedDeals");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<FeaturedDealResponseDto>>();
        Assert.NotNull(result);
        Assert.True(result.FindIndex(item => item.HotelId == popular.Hotel.HotelId) <
                    result.FindIndex(item => item.HotelId == ignored.Hotel.HotelId));
    }

    private async Task<(Hotel Hotel, Room Room)> CreateEligibleDealAsync()
    {
        var hotel = await CreateHotelAsync();
        var room = await CreateRoomAsync(hotel.HotelId, 100m);
        await CreatePromotionAsync(hotel.HotelId, 20);
        return (hotel, room);
    }

    private async Task<User> CreateUserAsync(
        Role role, CustomWebApplicationFactory? factory = null)
    {
        var host = factory ?? _factory;
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var user = new User("Test", "User", Unique("user"), $"{Guid.NewGuid():N}@test.com",
            hasher.HashPassword("Password123"), role);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private async Task<Hotel> CreateHotelAsync(string? cityName = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var city = new City(cityName ?? Unique("City"), "Palestine", Unique("PO"));
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

    private async Task<Promotion> CreatePromotionAsync(
        int hotelId, int discount, DateTime? start = null, DateTime? end = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var promotion = new Promotion(hotelId, discount,
            start ?? DateTime.UtcNow.AddDays(-1), end ?? DateTime.UtcNow.AddDays(1));
        db.Promotions.Add(promotion);
        await db.SaveChangesAsync();
        return promotion;
    }

    private async Task CreateHotelImagesAsync(int hotelId, params (string Url, int Order)[] images)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        db.HotelImages.AddRange(images.Select(image => new HotelImage(image.Url, image.Order, hotelId)));
        await db.SaveChangesAsync();
    }

    private async Task<Booking> CreateBookingAsync(
        User user, Hotel hotel, Room room, bool cancelled, DateTime createdAt)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var checkIn = new DateTime(2030, 1, 10);
        var checkOut = new DateTime(2030, 1, 12);
        var total = 2 * room.PricePerNight;
        var invoice = new Invoice(user.UserId, hotel.HotelId, total);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
        var booking = new Booking(user.UserId, room.RoomId, checkIn, checkOut, 2, 0,
            room.PricePerNight, total, 0, 0m, total, null)
        {
            InvoiceId = invoice.InvoiceId,
            CreatedAt = createdAt
        };
        if (cancelled) booking.Cancel();
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        return booking;
    }

    private async Task CreateReviewAsync(int bookingId, int rating)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        db.Reviews.Add(new Review(bookingId, rating, "Test review"));
        await db.SaveChangesAsync();
    }

    private async Task UpdateHotelAsync(int id, Action<Hotel> update)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var entity = await db.Hotels.FindAsync(id);
        Assert.NotNull(entity);
        update(entity);
        await db.SaveChangesAsync();
    }

    private async Task UpdateRoomAsync(int id, Action<Room> update)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var entity = await db.Rooms.FindAsync(id);
        Assert.NotNull(entity);
        update(entity);
        await db.SaveChangesAsync();
    }

    private async Task UpdatePromotionAsync(int id, Action<Promotion> update)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var entity = await db.Promotions.FindAsync(id);
        Assert.NotNull(entity);
        update(entity);
        await db.SaveChangesAsync();
    }

    private HttpClient CreateAuthenticatedClient(
        User user, CustomWebApplicationFactory? factory = null)
    {
        var host = factory ?? _factory;
        using var scope = host.Services.CreateScope();
        var generator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
        var client = host.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", generator.GenerateToken(user));
        return client;
    }

    private static string Unique(string prefix) => $"{prefix}_{Guid.NewGuid():N}";

    public enum IneligibleReason
    {
        InactiveHotel,
        NoRooms,
        InactiveRoom,
        OperationallyUnavailableRoom,
        NoPromotion,
        InactivePromotion,
        ExpiredPromotion,
        FuturePromotion
    }
}
