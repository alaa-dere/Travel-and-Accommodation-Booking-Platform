using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using HotelBooking.Application.HotelReviews.Dtos;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.IntegrationTests.Infrastructure;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.IntegrationTests.HotelReviews;

public class HotelReviewsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public HotelReviewsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData(ReviewEndpoint.Get)]
    [InlineData(ReviewEndpoint.Submit)]
    public async Task ReviewEndpoint_WithoutToken_ShouldReturnUnauthorized(ReviewEndpoint endpoint)
    {
        using var client = _factory.CreateClient();
        using var response = await SendAsync(client, endpoint, 1, ValidRequest(1));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(ReviewEndpoint.Get)]
    [InlineData(ReviewEndpoint.Submit)]
    public async Task ReviewEndpoint_WithAdminToken_ShouldReturnForbidden(ReviewEndpoint endpoint)
    {
        var admin = await CreateUserAsync(Role.Admin);
        using var client = CreateAuthenticatedClient(admin);
        using var response = await SendAsync(client, endpoint, 1, ValidRequest(1));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(999999)]
    public async Task GetHotelReviews_WhenHotelDoesNotExist_ShouldReturnNotFound(int hotelId)
    {
        var customer = await CreateUserAsync(Role.Customer);
        using var client = CreateAuthenticatedClient(customer);
        var response = await client.GetAsync($"/api/hotels/{hotelId}/reviews");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetHotelReviews_WhenHotelIsInactive_ShouldReturnNotFound()
    {
        var customer = await CreateUserAsync(Role.Customer);
        var hotel = await CreateHotelAsync();
        await SetHotelActiveAsync(hotel.HotelId, false);
        using var client = CreateAuthenticatedClient(customer);
        var response = await client.GetAsync($"/api/hotels/{hotel.HotelId}/reviews");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetHotelReviews_WhenNoCompletedReviews_ShouldReturnNullRatingAndEmptyArray()
    {
        var customer = await CreateUserAsync(Role.Customer);
        var hotel = await CreateHotelAsync();
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync($"/api/hotels/{hotel.HotelId}/reviews");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<HotelReviewsResponseDto>();
        Assert.NotNull(result);
        Assert.Null(result.Rating);
        Assert.Empty(result.Reviews);
    }

    [Fact]
    public async Task GetHotelReviews_ShouldMapCompletedReviewsCalculateAverageAndOrderNewestFirst()
    {
        var customer = await CreateUserAsync(Role.Customer);
        var hotel = await CreateHotelAsync();
        var room = await CreateRoomAsync(hotel.HotelId);
        var olderBooking = await CreateBookingAsync(customer, hotel, room, BookingStatus.Completed);
        var newerBooking = await CreateBookingAsync(customer, hotel, room, BookingStatus.Completed);
        var older = await CreateReviewAsync(olderBooking.BookingId, 2, "Older", DateTime.UtcNow.AddDays(-2));
        var newer = await CreateReviewAsync(newerBooking.BookingId, 5, "Newer", DateTime.UtcNow.AddDays(-1));
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync($"/api/hotels/{hotel.HotelId}/reviews");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<HotelReviewsResponseDto>();
        Assert.NotNull(result);
        Assert.Equal(3.5, result.Rating);
        Assert.Collection(result.Reviews,
            review => AssertReview(review, newer),
            review => AssertReview(review, older));
    }

    [Fact]
    public async Task GetHotelReviews_ShouldExcludeNonCompletedBookingsAndOtherHotels()
    {
        var customer = await CreateUserAsync(Role.Customer);
        var hotel = await CreateHotelAsync();
        var otherHotel = await CreateHotelAsync();
        var room = await CreateRoomAsync(hotel.HotelId);
        var otherRoom = await CreateRoomAsync(otherHotel.HotelId);
        var completed = await CreateBookingAsync(customer, hotel, room, BookingStatus.Completed);
        var pending = await CreateBookingAsync(customer, hotel, room, BookingStatus.Pending);
        var other = await CreateBookingAsync(customer, otherHotel, otherRoom, BookingStatus.Completed);
        await CreateReviewAsync(completed.BookingId, 4, "Included");
        await CreateReviewAsync(pending.BookingId, 1, "Pending excluded");
        await CreateReviewAsync(other.BookingId, 2, "Other excluded");
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync($"/api/hotels/{hotel.HotelId}/reviews");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<HotelReviewsResponseDto>();
        Assert.NotNull(result);
        Assert.Equal(4d, result.Rating);
        var review = Assert.Single(result.Reviews);
        Assert.Equal("Included", review.Comment);
    }

    [Fact]
    public async Task SubmitReview_WithValidRequest_ShouldReturnNoContentAndPersistReview()
    {
        var customer = await CreateUserAsync(Role.Customer);
        var hotel = await CreateHotelAsync();
        var room = await CreateRoomAsync(hotel.HotelId);
        var booking = await CreateBookingAsync(customer, hotel, room, BookingStatus.Completed);
        using var client = CreateAuthenticatedClient(customer);
        var request = ValidRequest(booking.BookingId);

        var response = await client.PostAsJsonAsync($"/api/hotels/{hotel.HotelId}/reviews", request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var review = await db.Reviews.AsNoTracking().SingleAsync(item => item.BookingId == booking.BookingId);
        Assert.Equal(request.Rating, review.Rating);
        Assert.Equal(request.Comment, review.Comment);
        Assert.True(review.CreatedAt <= DateTime.UtcNow);
    }

    [Theory]
    [InlineData(0, "Valid comment")]
    [InlineData(6, "Valid comment")]
    [InlineData(5, "")]
    [InlineData(5, "   ")]
    public async Task SubmitReview_WithInvalidRatingOrComment_ShouldReturnBadRequest(int rating, string comment)
    {
        var customer = await CreateUserAsync(Role.Customer);
        using var client = CreateAuthenticatedClient(customer);
        var request = new SubmitReviewRequestDto { BookingId = 1, Rating = rating, Comment = comment };
        var response = await client.PostAsJsonAsync("/api/hotels/1/reviews", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{invalid-json}")]
    public async Task SubmitReview_WithEmptyOrMalformedBody_ShouldReturnBadRequest(string body)
    {
        var customer = await CreateUserAsync(Role.Customer);
        using var client = CreateAuthenticatedClient(customer);
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/hotels/1/reviews", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SubmitReview_WhenBookingDoesNotExist_ShouldReturnNotFound()
    {
        var customer = await CreateUserAsync(Role.Customer);
        var hotel = await CreateHotelAsync();
        using var client = CreateAuthenticatedClient(customer);
        var response = await client.PostAsJsonAsync(
            $"/api/hotels/{hotel.HotelId}/reviews", ValidRequest(999999));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SubmitReview_WhenBookingBelongsToAnotherCustomer_ShouldReturnBadRequest()
    {
        var owner = await CreateUserAsync(Role.Customer);
        var otherCustomer = await CreateUserAsync(Role.Customer);
        var hotel = await CreateHotelAsync();
        var room = await CreateRoomAsync(hotel.HotelId);
        var booking = await CreateBookingAsync(owner, hotel, room, BookingStatus.Completed);
        using var client = CreateAuthenticatedClient(otherCustomer);
        var response = await client.PostAsJsonAsync(
            $"/api/hotels/{hotel.HotelId}/reviews", ValidRequest(booking.BookingId));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SubmitReview_WhenBookingBelongsToDifferentHotel_ShouldReturnBadRequest()
    {
        var customer = await CreateUserAsync(Role.Customer);
        var bookingHotel = await CreateHotelAsync();
        var requestedHotel = await CreateHotelAsync();
        var room = await CreateRoomAsync(bookingHotel.HotelId);
        var booking = await CreateBookingAsync(customer, bookingHotel, room, BookingStatus.Completed);
        using var client = CreateAuthenticatedClient(customer);
        var response = await client.PostAsJsonAsync(
            $"/api/hotels/{requestedHotel.HotelId}/reviews", ValidRequest(booking.BookingId));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(BookingStatus.Pending)]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Cancelled)]
    public async Task SubmitReview_WhenBookingIsNotCompleted_ShouldReturnBadRequest(BookingStatus status)
    {
        var customer = await CreateUserAsync(Role.Customer);
        var hotel = await CreateHotelAsync();
        var room = await CreateRoomAsync(hotel.HotelId);
        var booking = await CreateBookingAsync(customer, hotel, room, status);
        using var client = CreateAuthenticatedClient(customer);
        var response = await client.PostAsJsonAsync(
            $"/api/hotels/{hotel.HotelId}/reviews", ValidRequest(booking.BookingId));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SubmitReview_WhenCompletedBookingCheckoutIsInFuture_ShouldReturnBadRequest()
    {
        var customer = await CreateUserAsync(Role.Customer);
        var hotel = await CreateHotelAsync();
        var room = await CreateRoomAsync(hotel.HotelId);
        var booking = await CreateBookingAsync(customer, hotel, room, BookingStatus.Completed,
            DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(3));
        using var client = CreateAuthenticatedClient(customer);
        var response = await client.PostAsJsonAsync(
            $"/api/hotels/{hotel.HotelId}/reviews", ValidRequest(booking.BookingId));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SubmitReview_WhenReviewAlreadyExists_ShouldReturnBadRequestAndPreserveOriginal()
    {
        var customer = await CreateUserAsync(Role.Customer);
        var hotel = await CreateHotelAsync();
        var room = await CreateRoomAsync(hotel.HotelId);
        var booking = await CreateBookingAsync(customer, hotel, room, BookingStatus.Completed);
        await CreateReviewAsync(booking.BookingId, 3, "Original");
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.PostAsJsonAsync(
            $"/api/hotels/{hotel.HotelId}/reviews", ValidRequest(booking.BookingId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var review = await db.Reviews.AsNoTracking().SingleAsync(item => item.BookingId == booking.BookingId);
        Assert.Equal(3, review.Rating);
        Assert.Equal("Original", review.Comment);
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

    private async Task<Room> CreateRoomAsync(int hotelId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var room = new Room(Unique("Room"), RoomType.Double, 100m, 2, 1, hotelId, "Room");
        db.Rooms.Add(room);
        await db.SaveChangesAsync();
        return room;
    }

    private async Task<Booking> CreateBookingAsync(
        User user,
        Hotel hotel,
        Room room,
        BookingStatus status,
        DateTime? checkIn = null,
        DateTime? checkOut = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var actualCheckIn = checkIn ?? DateTime.UtcNow.AddDays(-5);
        var actualCheckOut = checkOut ?? DateTime.UtcNow.AddDays(-2);
        var total = (actualCheckOut.Date - actualCheckIn.Date).Days * 100m;
        var invoice = new Invoice(user.UserId, hotel.HotelId, total);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
        var booking = new Booking(user.UserId, room.RoomId, actualCheckIn, actualCheckOut,
            2, 0, 100m, total, 0, 0m, total, null)
        {
            InvoiceId = invoice.InvoiceId,
            BookingStatus = status
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        return booking;
    }

    private async Task<Review> CreateReviewAsync(
        int bookingId, int rating, string comment, DateTime? createdAt = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var review = new Review(bookingId, rating, comment);
        if (createdAt.HasValue) review.CreatedAt = createdAt.Value;
        db.Reviews.Add(review);
        await db.SaveChangesAsync();
        return review;
    }

    private async Task SetHotelActiveAsync(int hotelId, bool isActive)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var hotel = await db.Hotels.FindAsync(hotelId);
        Assert.NotNull(hotel);
        hotel.ChangeStatus(isActive);
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

    private static SubmitReviewRequestDto ValidRequest(int bookingId) => new()
    {
        BookingId = bookingId,
        Rating = 5,
        Comment = "Excellent hotel."
    };

    private static void AssertReview(ReviewResponseDto actual, Review expected)
    {
        Assert.Equal(expected.Rating, actual.Rating);
        Assert.Equal(expected.Comment, actual.Comment);
        Assert.Equal(expected.CreatedAt, actual.CreatedAt);
    }

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client, ReviewEndpoint endpoint, int hotelId, SubmitReviewRequestDto request) => endpoint switch
    {
        ReviewEndpoint.Get => await client.GetAsync($"/api/hotels/{hotelId}/reviews"),
        _ => await client.PostAsJsonAsync($"/api/hotels/{hotelId}/reviews", request)
    };

    private static string Unique(string prefix) => $"{prefix}_{Guid.NewGuid():N}";

    public enum ReviewEndpoint { Get, Submit }
}
