using System.Net;
using System.Net.Http.Headers;
using System.Text;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Enums;
using HotelBooking.IntegrationTests.Infrastructure;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.IntegrationTests.Invoices;

public class InvoicesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public InvoicesControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DownloadPdf_WithoutToken_ShouldReturnUnauthorized()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/invoices/1/pdf");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DownloadPdf_WithAdminToken_ShouldReturnForbidden()
    {
        var admin = await CreateUserAsync(Role.Admin);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.GetAsync("/api/invoices/1/pdf");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task DownloadPdf_WithInvalidInvoiceId_ShouldReturnBadRequest(int invoiceId)
    {
        var customer = await CreateUserAsync(Role.Customer);
        using var client = CreateAuthenticatedClient(customer);
        var response = await client.GetAsync($"/api/invoices/{invoiceId}/pdf");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DownloadPdf_WithNonNumericInvoiceId_ShouldReturnNotFound()
    {
        var customer = await CreateUserAsync(Role.Customer);
        using var client = CreateAuthenticatedClient(customer);
        var response = await client.GetAsync("/api/invoices/not-a-number/pdf");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DownloadPdf_WhenInvoiceDoesNotExist_ShouldReturnNotFound()
    {
        var customer = await CreateUserAsync(Role.Customer);
        using var client = CreateAuthenticatedClient(customer);
        var response = await client.GetAsync("/api/invoices/999999/pdf");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DownloadPdf_WhenInvoiceBelongsToDifferentCustomer_ShouldReturnNotFound()
    {
        var owner = await CreateUserAsync(Role.Customer);
        var otherCustomer = await CreateUserAsync(Role.Customer);
        var hotel = await CreateHotelAsync();
        var invoice = await CreateInvoiceAsync(owner, hotel, 200m, PaymentStatus.Paid);
        using var client = CreateAuthenticatedClient(otherCustomer);

        var response = await client.GetAsync($"/api/invoices/{invoice.InvoiceId}/pdf");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(PaymentStatus.Paid)]
    [InlineData(PaymentStatus.Failed)]
    public async Task DownloadPdf_WithValidInvoice_ShouldReturnPdfAttachment(PaymentStatus paymentStatus)
    {
        var customer = await CreateUserAsync(Role.Customer);
        var hotel = await CreateHotelAsync();
        var room = await CreateRoomAsync(hotel.HotelId, "101", 100m);
        var invoice = await CreateInvoiceAsync(customer, hotel, 270m, paymentStatus);
        await CreateBookingAsync(customer, invoice, room,
            new DateTime(2030, 1, 10), new DateTime(2030, 1, 13),
            100m, 300m, 10, 30m, 270m);
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync($"/api/invoices/{invoice.InvoiceId}/pdf");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal($"invoice-{invoice.InvoiceId}.pdf",
            response.Content.Headers.ContentDisposition?.FileNameStar ??
            response.Content.Headers.ContentDisposition?.FileName?.Trim('"'));
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition?.DispositionType);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 500);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public async Task DownloadPdf_WithMultipleBookings_ShouldGeneratePdfContainingAllLines()
    {
        var customer = await CreateUserAsync(Role.Customer);
        var hotel = await CreateHotelAsync();
        var firstRoom = await CreateRoomAsync(hotel.HotelId, "201", 80m);
        var secondRoom = await CreateRoomAsync(hotel.HotelId, "202", 120m);
        var invoice = await CreateInvoiceAsync(customer, hotel, 400m, PaymentStatus.Paid);
        await CreateBookingAsync(customer, invoice, firstRoom,
            new DateTime(2030, 2, 1), new DateTime(2030, 2, 3),
            80m, 160m, 0, 0m, 160m);
        await CreateBookingAsync(customer, invoice, secondRoom,
            new DateTime(2030, 2, 5), new DateTime(2030, 2, 7),
            120m, 240m, 0, 0m, 240m);
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync($"/api/invoices/{invoice.InvoiceId}/pdf");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 500);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        Assert.Equal(2, await db.Bookings.CountAsync(item => item.InvoiceId == invoice.InvoiceId));
    }

    [Fact]
    public async Task DownloadPdf_WhenInvoiceHasNoBookings_ShouldStillReturnPdf()
    {
        var customer = await CreateUserAsync(Role.Customer);
        var hotel = await CreateHotelAsync();
        var invoice = await CreateInvoiceAsync(customer, hotel, 100m, PaymentStatus.Paid);
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync($"/api/invoices/{invoice.InvoiceId}/pdf");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 500);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public async Task DownloadPdf_ShouldNotModifyInvoiceData()
    {
        var customer = await CreateUserAsync(Role.Customer);
        var hotel = await CreateHotelAsync();
        var room = await CreateRoomAsync(hotel.HotelId, "301", 100m);
        var invoice = await CreateInvoiceAsync(customer, hotel, 200m, PaymentStatus.Paid);
        var booking = await CreateBookingAsync(customer, invoice, room,
            new DateTime(2030, 3, 1), new DateTime(2030, 3, 3),
            100m, 200m, 0, 0m, 200m);
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync($"/api/invoices/{invoice.InvoiceId}/pdf");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var storedInvoice = await db.Invoices.AsNoTracking().SingleAsync(item => item.InvoiceId == invoice.InvoiceId);
        var storedBooking = await db.Bookings.AsNoTracking().SingleAsync(item => item.BookingId == booking.BookingId);
        Assert.Equal(200m, storedInvoice.TotalAmount);
        Assert.Equal(200m, storedBooking.TotalPrice);
        Assert.Equal(BookingStatus.Pending, storedBooking.BookingStatus);
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

    private async Task<Room> CreateRoomAsync(int hotelId, string number, decimal price)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var room = new Room($"{number}-{Guid.NewGuid():N}", RoomType.Double, price, 2, 1,
            hotelId, "Room");
        db.Rooms.Add(room);
        await db.SaveChangesAsync();
        return room;
    }

    private async Task<Invoice> CreateInvoiceAsync(
        User user, Hotel hotel, decimal amount, PaymentStatus paymentStatus)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var invoice = new Invoice(user.UserId, hotel.HotelId, amount);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
        var payment = new Payment(amount) { InvoiceId = invoice.InvoiceId };
        if (paymentStatus == PaymentStatus.Paid) payment.MarkAsPaid();
        else payment.MarkAsFailed();
        db.Payments.Add(payment);
        await db.SaveChangesAsync();
        return invoice;
    }

    private async Task<Booking> CreateBookingAsync(
        User user,
        Invoice invoice,
        Room room,
        DateTime checkIn,
        DateTime checkOut,
        decimal pricePerNight,
        decimal originalTotal,
        int discountPercentage,
        decimal discountAmount,
        decimal total)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var booking = new Booking(user.UserId, room.RoomId, checkIn, checkOut, 2, 0,
            pricePerNight, originalTotal, discountPercentage, discountAmount, total, null)
        {
            InvoiceId = invoice.InvoiceId
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        return booking;
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

    private static string Unique(string prefix) => $"{prefix}_{Guid.NewGuid():N}";
}
