using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HotelBooking.Application.AvailableRooms.Dtos;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.IntegrationTests.Infrastructure;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.IntegrationTests.AvailableRooms;

public class AvailableRoomsControllerTests :
    IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public AvailableRoomsControllerTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAvailableRooms_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(
            "/api/hotels/1/available-rooms" +
            "?CheckIn=2030-01-10&CheckOut=2030-01-12&Adults=2&Children=0");

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetAvailableRooms_WithAdminToken_ShouldReturnForbidden()
    {
        // Arrange
        var admin = await CreateUserAsync(
            Unique("admin"),
            UniqueEmail(),
            Role.Admin);

        using var client = CreateAuthenticatedClient(admin);

        // Act
        var response = await client.GetAsync(
            "/api/hotels/1/available-rooms" +
            "?CheckIn=2030-01-10&CheckOut=2030-01-12&Adults=2&Children=0");

        // Assert
        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task GetAvailableRooms_WithValidRequest_ShouldReturnAvailableRooms()
    {
        // Arrange
        var hotel = await CreateHotelAsync();

        var room = await CreateRoomAsync(
            hotel.HotelId,
            "101",
            2,
            1,
            100m);

        var customer = await CreateUserAsync(
            Unique("customer"),
            UniqueEmail(),
            Role.Customer);

        using var client = CreateAuthenticatedClient(customer);

        // Act
        var response = await client.GetAsync(
            $"/api/hotels/{hotel.HotelId}/available-rooms" +
            "?CheckIn=2030-01-10&CheckOut=2030-01-12&Adults=2&Children=1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<List<AvailableRoomResponseDto>>(JsonOptions);

        Assert.NotNull(result);

        var returnedRoom =
            Assert.Single(result.Where(r => r.RoomId == room.RoomId));

        Assert.Equal(room.RoomId, returnedRoom.RoomId);
        Assert.Equal(room.RoomType, returnedRoom.RoomType);
        Assert.Equal(room.AdultsCapacity, returnedRoom.AdultsCapacity);
        Assert.Equal(room.ChildCapacity, returnedRoom.ChildCapacity);
        Assert.Equal(room.PricePerNight, returnedRoom.PricePerNight);
    }

    [Fact]
    public async Task GetAvailableRooms_WhenCheckOutIsBeforeCheckIn_ShouldReturnBadRequest()
    {
        // Arrange
        var hotel = await CreateHotelAsync();

        var customer = await CreateUserAsync(
            Unique("customer"),
            UniqueEmail(),
            Role.Customer);

        using var client = CreateAuthenticatedClient(customer);

        // Act
        var response = await client.GetAsync(
            $"/api/hotels/{hotel.HotelId}/available-rooms" +
            "?CheckIn=2030-01-12&CheckOut=2030-01-10&Adults=2&Children=0");

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task GetAvailableRooms_WhenCheckOutEqualsCheckIn_ShouldReturnBadRequest()
    {
        // Arrange
        var hotel = await CreateHotelAsync();

        var customer = await CreateUserAsync(
            Unique("customer"),
            UniqueEmail(),
            Role.Customer);

        using var client = CreateAuthenticatedClient(customer);

        // Act
        var response = await client.GetAsync(
            $"/api/hotels/{hotel.HotelId}/available-rooms" +
            "?CheckIn=2030-01-10&CheckOut=2030-01-10&Adults=2&Children=0");

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task GetAvailableRooms_WhenAdultsIsZero_ShouldReturnBadRequest()
    {
        // Arrange
        var hotel = await CreateHotelAsync();

        var customer = await CreateUserAsync(
            Unique("customer"),
            UniqueEmail(),
            Role.Customer);

        using var client = CreateAuthenticatedClient(customer);

        // Act
        var response = await client.GetAsync(
            $"/api/hotels/{hotel.HotelId}/available-rooms" +
            "?CheckIn=2030-01-10&CheckOut=2030-01-12&Adults=0&Children=0");

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task GetAvailableRooms_WhenChildrenIsNegative_ShouldReturnBadRequest()
    {
        // Arrange
        var hotel = await CreateHotelAsync();

        var customer = await CreateUserAsync(
            Unique("customer"),
            UniqueEmail(),
            Role.Customer);

        using var client = CreateAuthenticatedClient(customer);

        // Act
        var response = await client.GetAsync(
            $"/api/hotels/{hotel.HotelId}/available-rooms" +
            "?CheckIn=2030-01-10&CheckOut=2030-01-12&Adults=2&Children=-1");

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task GetAvailableRooms_WhenHotelDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var customer = await CreateUserAsync(
            Unique("customer"),
            UniqueEmail(),
            Role.Customer);

        using var client = CreateAuthenticatedClient(customer);

        // Act
        var response = await client.GetAsync(
            "/api/hotels/999999/available-rooms" +
            "?CheckIn=2030-01-10&CheckOut=2030-01-12&Adults=2&Children=0");

        // Assert
        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task GetAvailableRooms_WhenHotelIsInactive_ShouldReturnNotFound()
    {
        // Arrange
        var hotel = await CreateHotelAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<HotelBookingDbContext>();

            var storedHotel =
                await dbContext.Hotels.FindAsync(hotel.HotelId);

            Assert.NotNull(storedHotel);

            storedHotel.ChangeStatus(false);

            await dbContext.SaveChangesAsync();
        }

        var customer = await CreateUserAsync(
            Unique("customer"),
            UniqueEmail(),
            Role.Customer);

        using var client = CreateAuthenticatedClient(customer);

        // Act
        var response = await client.GetAsync(
            $"/api/hotels/{hotel.HotelId}/available-rooms" +
            "?CheckIn=2030-01-10&CheckOut=2030-01-12&Adults=2&Children=0");

        // Assert
        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task GetAvailableRooms_WhenRoomIsInactive_ShouldNotReturnRoom()
    {
        // Arrange
        var hotel = await CreateHotelAsync();

        var room = await CreateRoomAsync(
            hotel.HotelId,
            "201",
            2,
            1,
            100m);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<HotelBookingDbContext>();

            var storedRoom =
                await dbContext.Rooms.FindAsync(room.RoomId);

            Assert.NotNull(storedRoom);

            storedRoom.ChangeStatus(false);

            await dbContext.SaveChangesAsync();
        }

        var customer = await CreateUserAsync(
            Unique("customer"),
            UniqueEmail(),
            Role.Customer);

        using var client = CreateAuthenticatedClient(customer);

        // Act
        var response = await client.GetAsync(
            $"/api/hotels/{hotel.HotelId}/available-rooms" +
            "?CheckIn=2030-01-10&CheckOut=2030-01-12&Adults=2&Children=0");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<List<AvailableRoomResponseDto>>(JsonOptions);

        Assert.NotNull(result);

        Assert.DoesNotContain(
            result,
            r => r.RoomId == room.RoomId);
    }

    [Fact]
    public async Task GetAvailableRooms_WhenRoomIsOperationallyUnavailable_ShouldNotReturnRoom()
    {
        // Arrange
        var hotel = await CreateHotelAsync();

        var room = await CreateRoomAsync(
            hotel.HotelId,
            "301",
            2,
            1,
            100m);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<HotelBookingDbContext>();

            var storedRoom =
                await dbContext.Rooms.FindAsync(room.RoomId);

            Assert.NotNull(storedRoom);

            storedRoom.ChangeOperationalAvailability(false);

            await dbContext.SaveChangesAsync();
        }

        var customer = await CreateUserAsync(
            Unique("customer"),
            UniqueEmail(),
            Role.Customer);

        using var client = CreateAuthenticatedClient(customer);

        // Act
        var response = await client.GetAsync(
            $"/api/hotels/{hotel.HotelId}/available-rooms" +
            "?CheckIn=2030-01-10&CheckOut=2030-01-12&Adults=2&Children=0");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<List<AvailableRoomResponseDto>>(JsonOptions);

        Assert.NotNull(result);

        Assert.DoesNotContain(
            result,
            r => r.RoomId == room.RoomId);
    }

    [Fact]
    public async Task GetAvailableRooms_WhenRoomCapacityIsTooSmall_ShouldNotReturnRoom()
    {
        // Arrange
        var hotel = await CreateHotelAsync();

        var room = await CreateRoomAsync(
            hotel.HotelId,
            "401",
            2,
            1,
            100m);

        var customer = await CreateUserAsync(
            Unique("customer"),
            UniqueEmail(),
            Role.Customer);

        using var client = CreateAuthenticatedClient(customer);

        // Act
        var response = await client.GetAsync(
            $"/api/hotels/{hotel.HotelId}/available-rooms" +
            "?CheckIn=2030-01-10&CheckOut=2030-01-12&Adults=3&Children=1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<List<AvailableRoomResponseDto>>(JsonOptions);

        Assert.NotNull(result);

        Assert.DoesNotContain(
            result,
            r => r.RoomId == room.RoomId);
    }

    [Fact]
    public async Task GetAvailableRooms_WhenChildCapacityIsTooSmall_ShouldNotReturnRoom()
    {
        var hotel = await CreateHotelAsync();
        var room = await CreateRoomAsync(hotel.HotelId, "402", 2, 1, 100m);
        var customer = await CreateUserAsync(Unique("customer"), UniqueEmail(), Role.Customer);
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync(
            $"/api/hotels/{hotel.HotelId}/available-rooms" +
            "?CheckIn=2030-01-10&CheckOut=2030-01-12&Adults=2&Children=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<AvailableRoomResponseDto>>(JsonOptions);
        Assert.NotNull(result);
        Assert.DoesNotContain(result, item => item.RoomId == room.RoomId);
    }

    [Fact]
    public async Task GetAvailableRooms_WhenCapacityExactlyMatches_ShouldReturnRoom()
    {
        var hotel = await CreateHotelAsync();
        var room = await CreateRoomAsync(hotel.HotelId, "403", 2, 1, 100m);
        var customer = await CreateUserAsync(Unique("customer"), UniqueEmail(), Role.Customer);
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync(
            $"/api/hotels/{hotel.HotelId}/available-rooms" +
            "?CheckIn=2030-01-10&CheckOut=2030-01-12&Adults=2&Children=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<AvailableRoomResponseDto>>(JsonOptions);
        Assert.NotNull(result);
        Assert.Contains(result, item => item.RoomId == room.RoomId);
    }

    [Theory]
    [InlineData("Adults=-1&Children=0")]
    [InlineData("Adults=2&Children=-1")]
    public async Task GetAvailableRooms_WithInvalidGuestCounts_ShouldReturnBadRequest(string guests)
    {
        var hotel = await CreateHotelAsync();
        var customer = await CreateUserAsync(Unique("customer"), UniqueEmail(), Role.Customer);
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync(
            $"/api/hotels/{hotel.HotelId}/available-rooms" +
            $"?CheckIn=2030-01-10&CheckOut=2030-01-12&{guests}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("?CheckOut=2030-01-12&Adults=2&Children=0")]
    [InlineData("?CheckIn=not-a-date&CheckOut=2030-01-12&Adults=2&Children=0")]
    public async Task GetAvailableRooms_WithMissingOrMalformedDate_ShouldReturnBadRequest(string query)
    {
        var hotel = await CreateHotelAsync();
        var customer = await CreateUserAsync(Unique("customer"), UniqueEmail(), Role.Customer);
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync($"/api/hotels/{hotel.HotelId}/available-rooms{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAvailableRooms_WhenNoRoomsMatch_ShouldReturnEmptyArray()
    {
        var hotel = await CreateHotelAsync();
        var customer = await CreateUserAsync(Unique("customer"), UniqueEmail(), Role.Customer);
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync(
            $"/api/hotels/{hotel.HotelId}/available-rooms" +
            "?CheckIn=2030-01-10&CheckOut=2030-01-12&Adults=2&Children=0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<AvailableRoomResponseDto>>(JsonOptions);
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAvailableRooms_ShouldMapDescriptionAndOrderImages()
    {
        var hotel = await CreateHotelAsync();
        var room = await CreateRoomAsync(hotel.HotelId, "404", 2, 1, 100m);
        await AddRoomImagesAsync(room.RoomId, ("second.jpg", 2), ("first.jpg", 1));
        var customer = await CreateUserAsync(Unique("customer"), UniqueEmail(), Role.Customer);
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync(
            $"/api/hotels/{hotel.HotelId}/available-rooms" +
            "?CheckIn=2030-01-10&CheckOut=2030-01-12&Adults=2&Children=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<AvailableRoomResponseDto>>(JsonOptions);
        Assert.NotNull(result);
        var returnedRoom = Assert.Single(result.Where(item => item.RoomId == room.RoomId));
        Assert.Equal("Test Room", returnedRoom.Description);
        Assert.Collection(returnedRoom.Images,
            image => { Assert.Equal("first.jpg", image.ImageUrl); Assert.Equal(1, image.DisplayOrder); },
            image => { Assert.Equal("second.jpg", image.ImageUrl); Assert.Equal(2, image.DisplayOrder); });
    }

    [Theory]
    [InlineData(false, "2030-01-11", "2030-01-12", false)]
    [InlineData(true, "2030-01-11", "2030-01-12", true)]
    [InlineData(false, "2030-01-12", "2030-01-14", true)]
    [InlineData(false, "2030-01-08", "2030-01-10", true)]
    public async Task GetAvailableRooms_WithExistingBooking_ShouldApplyOverlapRules(
        bool cancelled, string checkIn, string checkOut, bool expectedAvailable)
    {
        var hotel = await CreateHotelAsync();
        var room = await CreateRoomAsync(hotel.HotelId, "405", 2, 1, 100m);
        var customer = await CreateUserAsync(Unique("customer"), UniqueEmail(), Role.Customer);
        await CreateBookingAsync(customer, hotel, room, new DateTime(2030, 1, 10), new DateTime(2030, 1, 12), cancelled);
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.GetAsync(
            $"/api/hotels/{hotel.HotelId}/available-rooms" +
            $"?CheckIn={checkIn}&CheckOut={checkOut}&Adults=2&Children=0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<AvailableRoomResponseDto>>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(expectedAvailable, result.Any(item => item.RoomId == room.RoomId));
    }

    [Fact]
    public async Task SelectRoom_WhenRoomIsAvailable_ShouldReturnSelectedRoomWithTotalPrice()
    {
        // Arrange
        var hotel = await CreateHotelAsync();

        var room = await CreateRoomAsync(
            hotel.HotelId,
            "501",
            2,
            1,
            100m);

        var customer = await CreateUserAsync(
            Unique("customer"),
            UniqueEmail(),
            Role.Customer);

        using var client = CreateAuthenticatedClient(customer);

        var request = new AvailableRoomsRequestDto
        {
            CheckIn = new DateTime(2030, 1, 10),
            CheckOut = new DateTime(2030, 1, 13),
            Adults = 2,
            Children = 1
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/hotels/{hotel.HotelId}/available-rooms/{room.RoomId}/selection",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<SelectedRoomResponseDto>(JsonOptions);

        Assert.NotNull(result);

        Assert.Equal(room.RoomId, result.RoomId);
        Assert.Equal(room.RoomType, result.RoomType);
        Assert.Equal(request.CheckIn, result.CheckIn);
        Assert.Equal(request.CheckOut, result.CheckOut);
        Assert.Equal(request.Adults, result.Adults);
        Assert.Equal(request.Children, result.Children);
        Assert.Equal(100m, result.PricePerNight);

        // 3 nights × 100
        Assert.Equal(300m, result.TotalPrice);
    }

    [Fact]
    public async Task SelectRoom_WithoutToken_ShouldReturnUnauthorized()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/hotels/1/available-rooms/1/selection", ValidRequest());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SelectRoom_WithAdminToken_ShouldReturnForbidden()
    {
        var admin = await CreateUserAsync(Unique("admin"), UniqueEmail(), Role.Admin);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.PostAsJsonAsync(
            "/api/hotels/1/available-rooms/1/selection", ValidRequest());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(0, 0, 2030, 1, 10, 2030, 1, 12)]
    [InlineData(-1, 0, 2030, 1, 10, 2030, 1, 12)]
    [InlineData(2, -1, 2030, 1, 10, 2030, 1, 12)]
    [InlineData(2, 0, 2030, 1, 10, 2030, 1, 10)]
    public async Task SelectRoom_WithInvalidRequest_ShouldReturnBadRequest(
        int adults, int children,
        int checkInYear, int checkInMonth, int checkInDay,
        int checkOutYear, int checkOutMonth, int checkOutDay)
    {
        var hotel = await CreateHotelAsync();
        var room = await CreateRoomAsync(hotel.HotelId, "502", 2, 1, 100m);
        var customer = await CreateUserAsync(Unique("customer"), UniqueEmail(), Role.Customer);
        using var client = CreateAuthenticatedClient(customer);
        var request = new AvailableRoomsRequestDto
        {
            CheckIn = new DateTime(checkInYear, checkInMonth, checkInDay),
            CheckOut = new DateTime(checkOutYear, checkOutMonth, checkOutDay),
            Adults = adults,
            Children = children
        };

        var response = await client.PostAsJsonAsync(
            $"/api/hotels/{hotel.HotelId}/available-rooms/{room.RoomId}/selection", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{not-json}")]
    public async Task SelectRoom_WithEmptyOrMalformedBody_ShouldReturnBadRequest(string body)
    {
        var customer = await CreateUserAsync(Unique("customer"), UniqueEmail(), Role.Customer);
        using var client = CreateAuthenticatedClient(customer);
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/hotels/1/available-rooms/1/selection", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SelectRoom_WhenOperationallyUnavailable_ShouldReturnBadRequest()
    {
        var hotel = await CreateHotelAsync();
        var room = await CreateRoomAsync(hotel.HotelId, "503", 2, 1, 100m);
        await UpdateRoomAsync(room.RoomId, storedRoom => storedRoom.ChangeOperationalAvailability(false));
        var customer = await CreateUserAsync(Unique("customer"), UniqueEmail(), Role.Customer);
        using var client = CreateAuthenticatedClient(customer);

        var response = await client.PostAsJsonAsync(
            $"/api/hotels/{hotel.HotelId}/available-rooms/{room.RoomId}/selection", ValidRequest());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SelectRoom_WhenChildCapacityIsTooSmall_ShouldReturnBadRequest()
    {
        var hotel = await CreateHotelAsync();
        var room = await CreateRoomAsync(hotel.HotelId, "504", 2, 1, 100m);
        var customer = await CreateUserAsync(Unique("customer"), UniqueEmail(), Role.Customer);
        using var client = CreateAuthenticatedClient(customer);
        var request = ValidRequest();
        request.Children = 2;

        var response = await client.PostAsJsonAsync(
            $"/api/hotels/{hotel.HotelId}/available-rooms/{room.RoomId}/selection", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SelectRoom_WhenCapacityExactlyMatches_ShouldReturnOk()
    {
        var hotel = await CreateHotelAsync();
        var room = await CreateRoomAsync(hotel.HotelId, "505", 2, 1, 100m);
        var customer = await CreateUserAsync(Unique("customer"), UniqueEmail(), Role.Customer);
        using var client = CreateAuthenticatedClient(customer);
        var request = ValidRequest();
        request.Children = 1;

        var response = await client.PostAsJsonAsync(
            $"/api/hotels/{hotel.HotelId}/available-rooms/{room.RoomId}/selection", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData(false, "2030-01-11", "2030-01-12", HttpStatusCode.BadRequest)]
    [InlineData(true, "2030-01-11", "2030-01-12", HttpStatusCode.OK)]
    [InlineData(false, "2030-01-12", "2030-01-14", HttpStatusCode.OK)]
    [InlineData(false, "2030-01-08", "2030-01-10", HttpStatusCode.OK)]
    public async Task SelectRoom_WithExistingBooking_ShouldApplyOverlapRules(
        bool cancelled, string checkIn, string checkOut, HttpStatusCode expectedStatus)
    {
        var hotel = await CreateHotelAsync();
        var room = await CreateRoomAsync(hotel.HotelId, "506", 2, 1, 100m);
        var customer = await CreateUserAsync(Unique("customer"), UniqueEmail(), Role.Customer);
        await CreateBookingAsync(customer, hotel, room, new DateTime(2030, 1, 10), new DateTime(2030, 1, 12), cancelled);
        using var client = CreateAuthenticatedClient(customer);
        var request = ValidRequest();
        request.CheckIn = DateTime.Parse(checkIn);
        request.CheckOut = DateTime.Parse(checkOut);

        var response = await client.PostAsJsonAsync(
            $"/api/hotels/{hotel.HotelId}/available-rooms/{room.RoomId}/selection", request);

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Fact]
    public async Task SelectRoom_WhenRoomDoesNotExist_ShouldReturnBadRequest()
    {
        // Arrange
        var hotel = await CreateHotelAsync();

        var customer = await CreateUserAsync(
            Unique("customer"),
            UniqueEmail(),
            Role.Customer);

        using var client = CreateAuthenticatedClient(customer);

        var request = ValidRequest();

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/hotels/{hotel.HotelId}/available-rooms/999999/selection",
            request);

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task SelectRoom_WhenRoomBelongsToDifferentHotel_ShouldReturnBadRequest()
    {
        // Arrange
        var firstHotel = await CreateHotelAsync();
        var secondHotel = await CreateHotelAsync();

        var room = await CreateRoomAsync(
            firstHotel.HotelId,
            "601",
            2,
            1,
            100m);

        var customer = await CreateUserAsync(
            Unique("customer"),
            UniqueEmail(),
            Role.Customer);

        using var client = CreateAuthenticatedClient(customer);

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/hotels/{secondHotel.HotelId}/available-rooms/{room.RoomId}/selection",
            ValidRequest());

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task SelectRoom_WhenRoomCapacityIsTooSmall_ShouldReturnBadRequest()
    {
        // Arrange
        var hotel = await CreateHotelAsync();

        var room = await CreateRoomAsync(
            hotel.HotelId,
            "701",
            2,
            1,
            100m);

        var customer = await CreateUserAsync(
            Unique("customer"),
            UniqueEmail(),
            Role.Customer);

        using var client = CreateAuthenticatedClient(customer);

        var request = new AvailableRoomsRequestDto
        {
            CheckIn = new DateTime(2030, 1, 10),
            CheckOut = new DateTime(2030, 1, 12),
            Adults = 3,
            Children = 1
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/hotels/{hotel.HotelId}/available-rooms/{room.RoomId}/selection",
            request);

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task SelectRoom_WhenRoomIsInactive_ShouldReturnBadRequest()
    {
        // Arrange
        var hotel = await CreateHotelAsync();

        var room = await CreateRoomAsync(
            hotel.HotelId,
            "801",
            2,
            1,
            100m);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<HotelBookingDbContext>();

            var storedRoom =
                await dbContext.Rooms.FindAsync(room.RoomId);

            Assert.NotNull(storedRoom);

            storedRoom.ChangeStatus(false);

            await dbContext.SaveChangesAsync();
        }

        var customer = await CreateUserAsync(
            Unique("customer"),
            UniqueEmail(),
            Role.Customer);

        using var client = CreateAuthenticatedClient(customer);

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/hotels/{hotel.HotelId}/available-rooms/{room.RoomId}/selection",
            ValidRequest());

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task SelectRoom_WhenHotelIsInactive_ShouldReturnBadRequest()
    {
        // Arrange
        var hotel = await CreateHotelAsync();

        var room = await CreateRoomAsync(
            hotel.HotelId,
            "901",
            2,
            1,
            100m);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<HotelBookingDbContext>();

            var storedHotel =
                await dbContext.Hotels.FindAsync(hotel.HotelId);

            Assert.NotNull(storedHotel);

            storedHotel.ChangeStatus(false);

            await dbContext.SaveChangesAsync();
        }

        var customer = await CreateUserAsync(
            Unique("customer"),
            UniqueEmail(),
            Role.Customer);

        using var client = CreateAuthenticatedClient(customer);

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/hotels/{hotel.HotelId}/available-rooms/{room.RoomId}/selection",
            ValidRequest());

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task SelectRoom_WhenDatesAreInvalid_ShouldReturnBadRequest()
    {
        // Arrange
        var hotel = await CreateHotelAsync();

        var room = await CreateRoomAsync(
            hotel.HotelId,
            "1001",
            2,
            1,
            100m);

        var customer = await CreateUserAsync(
            Unique("customer"),
            UniqueEmail(),
            Role.Customer);

        using var client = CreateAuthenticatedClient(customer);

        var request = new AvailableRoomsRequestDto
        {
            CheckIn = new DateTime(2030, 1, 12),
            CheckOut = new DateTime(2030, 1, 10),
            Adults = 2,
            Children = 0
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/hotels/{hotel.HotelId}/available-rooms/{room.RoomId}/selection",
            request);

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    private async Task<User> CreateUserAsync(
        string username,
        string email,
        Role role)
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<HotelBookingDbContext>();

        var passwordHasher =
            scope.ServiceProvider
                .GetRequiredService<IPasswordHasher>();

        var user = new User(
            "Test",
            "User",
            username,
            email,
            passwordHasher.HashPassword("Password123"),
            role);

        dbContext.Users.Add(user);

        await dbContext.SaveChangesAsync();

        return user;
    }

    private async Task<Hotel> CreateHotelAsync()
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<HotelBookingDbContext>();

        var city = new City(
            Unique("City"),
            "Palestine",
            Unique("PO"));

        dbContext.Cities.Add(city);
        await dbContext.SaveChangesAsync();

        var hotel = new Hotel(
            Unique("Hotel"),
            "Test Owner",
            "Test Address",
            32.2211,
            35.2544,
            HotelType.Luxury,
            city.CityId,
            "Test Description",
            "Test History");

        dbContext.Hotels.Add(hotel);
        await dbContext.SaveChangesAsync();

        return hotel;
    }

    private async Task<Room> CreateRoomAsync(
        int hotelId,
        string roomNumber,
        int adultsCapacity,
        int childCapacity,
        decimal pricePerNight)
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<HotelBookingDbContext>();

        var room = new Room(
            $"{roomNumber}-{Guid.NewGuid():N}",
            RoomType.Double,
            pricePerNight,
            adultsCapacity,
            childCapacity,
            hotelId,
            "Test Room");

        dbContext.Rooms.Add(room);

        await dbContext.SaveChangesAsync();

        return room;
    }

    private async Task AddRoomImagesAsync(int roomId, params (string Url, int DisplayOrder)[] images)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        dbContext.RoomImages.AddRange(images.Select(image =>
            new RoomImage(image.Url, image.DisplayOrder, roomId)));
        await dbContext.SaveChangesAsync();
    }

    private async Task UpdateRoomAsync(int roomId, Action<Room> update)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var room = await dbContext.Rooms.FindAsync(roomId);
        Assert.NotNull(room);
        update(room);
        await dbContext.SaveChangesAsync();
    }

    private async Task CreateBookingAsync(
        User user, Hotel hotel, Room room, DateTime checkIn, DateTime checkOut, bool cancelled)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var nights = (checkOut.Date - checkIn.Date).Days;
        var total = nights * room.PricePerNight;
        var invoice = new Invoice(user.UserId, hotel.HotelId, total);
        dbContext.Invoices.Add(invoice);
        await dbContext.SaveChangesAsync();

        var booking = new Booking(
            user.UserId, room.RoomId, checkIn, checkOut, 2, 0,
            room.PricePerNight, total, 0, 0m, total, null)
        {
            InvoiceId = invoice.InvoiceId
        };
        if (cancelled)
        {
            booking.Cancel();
        }

        dbContext.Bookings.Add(booking);
        await dbContext.SaveChangesAsync();
    }

    private HttpClient CreateAuthenticatedClient(User user)
    {
        using var scope = _factory.Services.CreateScope();

        var jwtTokenGenerator =
            scope.ServiceProvider
                .GetRequiredService<IJwtTokenGenerator>();

        var token = jwtTokenGenerator.GenerateToken(user);

        var client = _factory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);

        return client;
    }

    private static AvailableRoomsRequestDto ValidRequest()
    {
        return new AvailableRoomsRequestDto
        {
            CheckIn = new DateTime(2030, 1, 10),
            CheckOut = new DateTime(2030, 1, 12),
            Adults = 2,
            Children = 0
        };
    }

    private static string Unique(string prefix)
    {
        return $"{prefix}_{Guid.NewGuid():N}";
    }

    private static string UniqueEmail()
    {
        return $"{Guid.NewGuid():N}@test.com";
    }
}
