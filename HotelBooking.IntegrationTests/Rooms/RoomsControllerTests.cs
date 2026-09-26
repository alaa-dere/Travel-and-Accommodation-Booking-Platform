using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Rooms.Dtos;
using HotelBooking.Domain.Entities;
using HotelBooking.IntegrationTests.Infrastructure;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.IntegrationTests.Rooms;

public class RoomsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public RoomsControllerTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Theory]
    [InlineData(RoomEndpoint.Get)]
    [InlineData(RoomEndpoint.Create)]
    [InlineData(RoomEndpoint.Update)]
    [InlineData(RoomEndpoint.Status)]
    [InlineData(RoomEndpoint.OperationalAvailability)]
    [InlineData(RoomEndpoint.AddImage)]
    [InlineData(RoomEndpoint.DeleteImage)]
    public async Task RoomEndpoint_WithoutToken_ShouldReturnUnauthorized(RoomEndpoint endpoint)
    {
        using var client = _factory.CreateClient();
        using var response = await SendAsync(client, endpoint, 1, 1);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(RoomEndpoint.Get)]
    [InlineData(RoomEndpoint.Create)]
    [InlineData(RoomEndpoint.Update)]
    [InlineData(RoomEndpoint.Status)]
    [InlineData(RoomEndpoint.OperationalAvailability)]
    [InlineData(RoomEndpoint.AddImage)]
    [InlineData(RoomEndpoint.DeleteImage)]
    public async Task RoomEndpoint_WithCustomerToken_ShouldReturnForbidden(RoomEndpoint endpoint)
    {
        var customer = await CreateUserAsync(Role.Customer);
        using var client = CreateAuthenticatedClient(customer);
        using var response = await SendAsync(client, endpoint, 1, 1);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetRooms_ShouldReturnRoomsWithAllFieldsAndImagesOrderedByDisplayOrder()
    {
        var admin = await CreateUserAsync(Role.Admin);
        var hotel = await CreateHotelAsync();
        var room = await CreateRoomAsync(hotel.HotelId, "Mapped", RoomType.Suite, 175.50m, 3, 2, "Description");
        await CreateImageAsync(room.RoomId, "https://test/second.jpg", 2);
        await CreateImageAsync(room.RoomId, "https://test/first.jpg", 1);
        using var client = CreateAuthenticatedClient(admin);

        var response = await client.GetAsync($"/api/Rooms?search={room.RoomNumber}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await ReadRoomsAsync(response);
        var mapped = Assert.Single(result);
        Assert.Equal(room.RoomId, mapped.RoomId);
        Assert.Equal(hotel.HotelId, mapped.HotelId);
        Assert.Equal(RoomType.Suite, mapped.RoomType);
        Assert.Equal(3, mapped.AdultsCapacity);
        Assert.Equal(2, mapped.ChildCapacity);
        Assert.Equal(175.50m, mapped.PricePerNight);
        Assert.Equal("Description", mapped.Description);
        Assert.True(mapped.IsActive);
        Assert.True(mapped.IsOperationallyAvailable);
        Assert.Equal(new[] { 1, 2 }, mapped.Images.Select(image => image.DisplayOrder));
    }

    [Fact]
    public async Task GetRooms_WhenNoRoomsMatch_ShouldReturnEmptyArray()
    {
        var admin = await CreateUserAsync(Role.Admin);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.GetAsync($"/api/Rooms?search={Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(await ReadRoomsAsync(response));
    }

    [Theory]
    [InlineData(RoomFilterKind.Search)]
    [InlineData(RoomFilterKind.Hotel)]
    [InlineData(RoomFilterKind.Type)]
    [InlineData(RoomFilterKind.Active)]
    [InlineData(RoomFilterKind.Operational)]
    public async Task GetRooms_ShouldApplyEachFilter(RoomFilterKind filterKind)
    {
        var admin = await CreateUserAsync(Role.Admin);
        var firstHotel = await CreateHotelAsync();
        var secondHotel = await CreateHotelAsync();
        var token = Unique("Match");
        var matching = await CreateRoomAsync(firstHotel.HotelId, token, RoomType.Suite, 100m, 2, 0);
        var other = await CreateRoomAsync(secondHotel.HotelId, Unique("Other"), RoomType.Single, 100m, 2, 0);
        if (filterKind == RoomFilterKind.Active) matching.ChangeStatus(false);
        if (filterKind == RoomFilterKind.Operational) matching.ChangeOperationalAvailability(false);
        await SaveRoomAsync(matching);
        using var client = CreateAuthenticatedClient(admin);
        var query = filterKind switch
        {
            RoomFilterKind.Search => $"search={token}",
            RoomFilterKind.Hotel => $"hotelId={firstHotel.HotelId}",
            RoomFilterKind.Type => "roomType=Suite",
            RoomFilterKind.Active => "isActive=false",
            _ => "isOperationallyAvailable=false"
        };

        var response = await client.GetAsync($"/api/Rooms?{query}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await ReadRoomsAsync(response);
        Assert.Contains(result, item => item.RoomId == matching.RoomId);
        Assert.DoesNotContain(result, item => item.RoomId == other.RoomId);
    }

    [Fact]
    public async Task GetRooms_ShouldCombineFilters()
    {
        var admin = await CreateUserAsync(Role.Admin);
        var hotel = await CreateHotelAsync();
        var matching = await CreateRoomAsync(hotel.HotelId, Unique("Combined"), RoomType.Double, 100m, 2, 0);
        var wrongType = await CreateRoomAsync(hotel.HotelId, Unique("Combined"), RoomType.Single, 100m, 2, 0);
        matching.ChangeStatus(false);
        wrongType.ChangeStatus(false);
        await SaveRoomAsync(matching);
        await SaveRoomAsync(wrongType);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.GetAsync($"/api/Rooms?hotelId={hotel.HotelId}&roomType=Double&isActive=false");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(matching.RoomId, Assert.Single(await ReadRoomsAsync(response)).RoomId);
    }

    [Fact]
    public async Task CreateRoom_WithValidRequest_ShouldReturnCreatedAndPersistMappedRoom()
    {
        var admin = await CreateUserAsync(Role.Admin);
        var hotel = await CreateHotelAsync();
        var request = ValidRequest(hotel.HotelId);
        using var client = CreateAuthenticatedClient(admin);

        var response = await client.PostAsJsonAsync("/api/Rooms", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("api/rooms", response.Headers.Location?.OriginalString);
        var mapped = await response.Content.ReadFromJsonAsync<RoomResponseDto>(JsonOptions);
        Assert.NotNull(mapped);
        Assert.Equal(request.RoomNumber, mapped.RoomNumber);
        Assert.Equal(request.RoomType, mapped.RoomType);
        Assert.Equal(request.PricePerNight, mapped.PricePerNight);
        Assert.True(mapped.IsActive);
        Assert.True(mapped.IsOperationallyAvailable);
        var stored = await FindRoomAsync(mapped.RoomId);
        Assert.NotNull(stored);
        Assert.Equal(request.Description, stored.Description);
    }

    [Fact]
    public async Task CreateRoom_WhenHotelDoesNotExist_ShouldReturnNotFoundAndNotPersistRoom()
    {
        var admin = await CreateUserAsync(Role.Admin);
        var request = ValidRequest(999999);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.PostAsJsonAsync("/api/Rooms", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Null(await FindRoomByNumberAsync(request.RoomNumber));
    }

    [Theory]
    [InlineData(InvalidRoomRequest.ZeroHotel)]
    [InlineData(InvalidRoomRequest.EmptyNumber)]
    [InlineData(InvalidRoomRequest.WhitespaceNumber)]
    [InlineData(InvalidRoomRequest.NumberTooLong)]
    [InlineData(InvalidRoomRequest.InvalidType)]
    [InlineData(InvalidRoomRequest.ZeroAdults)]
    [InlineData(InvalidRoomRequest.NegativeChildren)]
    [InlineData(InvalidRoomRequest.ZeroPrice)]
    public async Task CreateRoom_WithInvalidRequest_ShouldReturnBadRequest(InvalidRoomRequest invalid)
    {
        var admin = await CreateUserAsync(Role.Admin);
        var hotel = await CreateHotelAsync();
        var request = ValidRequest(hotel.HotelId);
        MakeInvalid(request, invalid);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.PostAsJsonAsync("/api/Rooms", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateRoom_WithValidRequest_ShouldReturnAndPersistAllChangedFields()
    {
        var admin = await CreateUserAsync(Role.Admin);
        var originalHotel = await CreateHotelAsync();
        var targetHotel = await CreateHotelAsync();
        var room = await CreateRoomAsync(originalHotel.HotelId);
        var request = ValidRequest(targetHotel.HotelId);
        request.RoomNumber = Unique("Updated");
        request.RoomType = RoomType.Suite;
        request.AdultsCapacity = 4;
        request.ChildCapacity = 3;
        request.PricePerNight = 250m;
        request.Description = "Updated description";
        using var client = CreateAuthenticatedClient(admin);

        var response = await client.PutAsJsonAsync($"/api/Rooms/{room.RoomId}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var mapped = await response.Content.ReadFromJsonAsync<RoomResponseDto>(JsonOptions);
        Assert.NotNull(mapped);
        Assert.Equal(request.RoomNumber, mapped.RoomNumber);
        Assert.Equal(targetHotel.HotelId, mapped.HotelId);
        var stored = await FindRoomAsync(room.RoomId);
        Assert.NotNull(stored);
        Assert.Equal(request.PricePerNight, stored.PricePerNight);
        Assert.NotNull(stored.UpdatedAt);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateRoom_WhenRoomOrHotelDoesNotExist_ShouldReturnNotFound(bool missingRoom)
    {
        var admin = await CreateUserAsync(Role.Admin);
        var hotel = await CreateHotelAsync();
        var room = await CreateRoomAsync(hotel.HotelId);
        var request = ValidRequest(missingRoom ? hotel.HotelId : 999999);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.PutAsJsonAsync($"/api/Rooms/{(missingRoom ? 999999 : room.RoomId)}", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateRoom_WithInvalidRequest_ShouldReturnBadRequestAndLeaveRoomUnchanged()
    {
        var admin = await CreateUserAsync(Role.Admin);
        var hotel = await CreateHotelAsync();
        var room = await CreateRoomAsync(hotel.HotelId);
        var request = ValidRequest(hotel.HotelId);
        request.PricePerNight = 0;
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.PutAsJsonAsync($"/api/Rooms/{room.RoomId}", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(room.PricePerNight, (await FindRoomAsync(room.RoomId))!.PricePerNight);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ChangeStatus_ShouldPersistRequestedValue(bool isActive)
    {
        var admin = await CreateUserAsync(Role.Admin);
        var room = await CreateRoomAsync((await CreateHotelAsync()).HotelId);
        room.ChangeStatus(!isActive);
        await SaveRoomAsync(room);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.PatchAsJsonAsync($"/api/Rooms/{room.RoomId}/status", new ChangeRoomStatusRequest { IsActive = isActive });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(isActive, (await FindRoomAsync(room.RoomId))!.IsActive);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ChangeOperationalAvailability_ShouldPersistRequestedValue(bool available)
    {
        var admin = await CreateUserAsync(Role.Admin);
        var room = await CreateRoomAsync((await CreateHotelAsync()).HotelId);
        room.ChangeOperationalAvailability(!available);
        await SaveRoomAsync(room);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.PatchAsJsonAsync($"/api/Rooms/{room.RoomId}/operational-availability", new ChangeRoomOperationalAvailabilityRequest { IsOperationallyAvailable = available });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(available, (await FindRoomAsync(room.RoomId))!.IsOperationallyAvailable);
    }

    [Theory]
    [InlineData("status")]
    [InlineData("operational-availability")]
    public async Task ChangeRoomFlag_WhenRoomDoesNotExist_ShouldReturnNotFound(string action)
    {
        var admin = await CreateUserAsync(Role.Admin);
        using var client = CreateAuthenticatedClient(admin);
        var content = action == "status" ? JsonContent.Create(new ChangeRoomStatusRequest()) : JsonContent.Create(new ChangeRoomOperationalAvailabilityRequest());
        var response = await client.PatchAsync($"/api/Rooms/999999/{action}", content);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddImage_WithValidRequest_ShouldReturnNoContentAndPersistImage()
    {
        var admin = await CreateUserAsync(Role.Admin);
        var room = await CreateRoomAsync((await CreateHotelAsync()).HotelId);
        var request = new AddRoomImageRequestDto { ImageUrl = "https://test/image.jpg", DisplayOrder = 2 };
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.PostAsJsonAsync($"/api/Rooms/{room.RoomId}/images", request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var image = await FindImageByUrlAsync(request.ImageUrl);
        Assert.NotNull(image);
        Assert.Equal(room.RoomId, image.RoomId);
        Assert.Equal(2, image.DisplayOrder);
    }

    [Fact]
    public async Task AddImage_WhenRoomDoesNotExist_ShouldReturnNotFound()
    {
        var admin = await CreateUserAsync(Role.Admin);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.PostAsJsonAsync("/api/Rooms/999999/images", new AddRoomImageRequestDto { ImageUrl = "https://test/image.jpg", DisplayOrder = 1 });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("", 1)]
    [InlineData("   ", 1)]
    [InlineData("https://test/image.jpg", 0)]
    public async Task AddImage_WithInvalidRequest_ShouldReturnBadRequest(string imageUrl, int displayOrder)
    {
        var admin = await CreateUserAsync(Role.Admin);
        var room = await CreateRoomAsync((await CreateHotelAsync()).HotelId);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.PostAsJsonAsync($"/api/Rooms/{room.RoomId}/images", new AddRoomImageRequestDto { ImageUrl = imageUrl, DisplayOrder = displayOrder });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteImage_WithMatchingRoomAndImage_ShouldReturnNoContentAndDeleteImage()
    {
        var admin = await CreateUserAsync(Role.Admin);
        var room = await CreateRoomAsync((await CreateHotelAsync()).HotelId);
        var image = await CreateImageAsync(room.RoomId, "https://test/delete.jpg", 1);
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.DeleteAsync($"/api/Rooms/{room.RoomId}/images/{image.RoomImageId}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(await FindImageAsync(image.RoomImageId));
    }

    [Theory]
    [InlineData(DeleteImageFailure.MissingRoom)]
    [InlineData(DeleteImageFailure.MissingImage)]
    [InlineData(DeleteImageFailure.ImageBelongsToOtherRoom)]
    public async Task DeleteImage_WhenTargetIsInvalid_ShouldReturnNotFoundAndPreserveExistingImage(DeleteImageFailure failure)
    {
        var admin = await CreateUserAsync(Role.Admin);
        var hotel = await CreateHotelAsync();
        var room = await CreateRoomAsync(hotel.HotelId);
        var otherRoom = await CreateRoomAsync(hotel.HotelId);
        var image = await CreateImageAsync(room.RoomId, "https://test/keep.jpg", 1);
        var roomId = failure == DeleteImageFailure.MissingRoom ? 999999 : otherRoom.RoomId;
        var imageId = failure == DeleteImageFailure.MissingImage ? 999999 : image.RoomImageId;
        using var client = CreateAuthenticatedClient(admin);
        var response = await client.DeleteAsync($"/api/Rooms/{roomId}/images/{imageId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(await FindImageAsync(image.RoomImageId));
    }

    private async Task<HttpResponseMessage> SendAsync(HttpClient client, RoomEndpoint endpoint, int roomId, int imageId) => endpoint switch
    {
        RoomEndpoint.Get => await client.GetAsync("/api/Rooms"),
        RoomEndpoint.Create => await client.PostAsJsonAsync("/api/Rooms", ValidRequest(1)),
        RoomEndpoint.Update => await client.PutAsJsonAsync($"/api/Rooms/{roomId}", ValidRequest(1)),
        RoomEndpoint.Status => await client.PatchAsJsonAsync($"/api/Rooms/{roomId}/status", new ChangeRoomStatusRequest()),
        RoomEndpoint.OperationalAvailability => await client.PatchAsJsonAsync($"/api/Rooms/{roomId}/operational-availability", new ChangeRoomOperationalAvailabilityRequest()),
        RoomEndpoint.AddImage => await client.PostAsJsonAsync($"/api/Rooms/{roomId}/images", new AddRoomImageRequestDto { ImageUrl = "https://test/image.jpg", DisplayOrder = 1 }),
        _ => await client.DeleteAsync($"/api/Rooms/{roomId}/images/{imageId}")
    };

    private async Task<User> CreateUserAsync(Role role)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var user = new User("Test", "User", Unique("user"), $"{Guid.NewGuid():N}@test.com", hasher.HashPassword("Password123"), role);
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
        var hotel = new Hotel(Unique("Hotel"), "Owner", "Address", 32.2, 35.2, HotelType.Luxury, city.CityId, null, null);
        db.Hotels.Add(hotel);
        await db.SaveChangesAsync();
        return hotel;
    }

    private async Task<Room> CreateRoomAsync(int hotelId, string? number = null, RoomType type = RoomType.Double, decimal price = 100m, int adults = 2, int children = 0, string? description = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var room = new Room(number ?? Unique("Room"), type, price, adults, children, hotelId, description);
        db.Rooms.Add(room);
        await db.SaveChangesAsync();
        return room;
    }

    private async Task SaveRoomAsync(Room room)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        db.Rooms.Update(room);
        await db.SaveChangesAsync();
    }

    private async Task<RoomImage> CreateImageAsync(int roomId, string url, int order)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>();
        var image = new RoomImage(url, order, roomId);
        db.RoomImages.Add(image);
        await db.SaveChangesAsync();
        return image;
    }

    private async Task<Room?> FindRoomAsync(int id)
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>().Rooms.AsNoTracking().SingleOrDefaultAsync(room => room.RoomId == id);
    }

    private async Task<Room?> FindRoomByNumberAsync(string number)
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>().Rooms.AsNoTracking().SingleOrDefaultAsync(room => room.RoomNumber == number);
    }

    private async Task<RoomImage?> FindImageAsync(int id)
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>().RoomImages.AsNoTracking().SingleOrDefaultAsync(image => image.RoomImageId == id);
    }

    private async Task<RoomImage?> FindImageByUrlAsync(string url)
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>().RoomImages.AsNoTracking().SingleOrDefaultAsync(image => image.ImageUrl == url);
    }

    private HttpClient CreateAuthenticatedClient(User user)
    {
        using var scope = _factory.Services.CreateScope();
        var generator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", generator.GenerateToken(user));
        return client;
    }

    private static async Task<List<RoomResponseDto>> ReadRoomsAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<List<RoomResponseDto>>(JsonOptions) ?? [];

    private static RoomRequestDto ValidRequest(int hotelId) => new()
    {
        HotelId = hotelId,
        RoomNumber = Unique("Room"),
        RoomType = RoomType.Double,
        AdultsCapacity = 2,
        ChildCapacity = 1,
        PricePerNight = 125m,
        Description = "Room description"
    };

    private static void MakeInvalid(RoomRequestDto request, InvalidRoomRequest invalid)
    {
        switch (invalid)
        {
            case InvalidRoomRequest.ZeroHotel: request.HotelId = 0; break;
            case InvalidRoomRequest.EmptyNumber: request.RoomNumber = ""; break;
            case InvalidRoomRequest.WhitespaceNumber: request.RoomNumber = "   "; break;
            case InvalidRoomRequest.NumberTooLong: request.RoomNumber = new string('x', 51); break;
            case InvalidRoomRequest.InvalidType: request.RoomType = (RoomType)999; break;
            case InvalidRoomRequest.ZeroAdults: request.AdultsCapacity = 0; break;
            case InvalidRoomRequest.NegativeChildren: request.ChildCapacity = -1; break;
            case InvalidRoomRequest.ZeroPrice: request.PricePerNight = 0; break;
        }
    }

    private static string Unique(string prefix) => $"{prefix}_{Guid.NewGuid():N}";

    public enum RoomEndpoint { Get, Create, Update, Status, OperationalAvailability, AddImage, DeleteImage }
    public enum RoomFilterKind { Search, Hotel, Type, Active, Operational }
    public enum InvalidRoomRequest { ZeroHotel, EmptyNumber, WhitespaceNumber, NumberTooLong, InvalidType, ZeroAdults, NegativeChildren, ZeroPrice }
    public enum DeleteImageFailure { MissingRoom, MissingImage, ImageBelongsToOtherRoom }
}
