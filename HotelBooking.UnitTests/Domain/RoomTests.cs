using HotelBooking.Domain.Entities;

namespace HotelBooking.UnitTests.Domain.Entities;

public class RoomTests
{
    [Fact]
    public void Constructor_WhenDataIsValid_ShouldCreateRoomWithCorrectValues()
    {
        var beforeCreation = DateTime.UtcNow;

        var room = CreateValidRoom();

        var afterCreation = DateTime.UtcNow;

        Assert.Equal("101", room.RoomNumber);
        Assert.Equal((RoomType)1, room.RoomType);
        Assert.Equal(100m, room.PricePerNight);
        Assert.Equal(2, room.AdultsCapacity);
        Assert.Equal(1, room.ChildCapacity);
        Assert.Equal(10, room.HotelId);
        Assert.Equal("Test room", room.Description);

        Assert.True(room.IsActive);
        Assert.True(room.IsOperationallyAvailable);
        Assert.Null(room.UpdatedAt);

        Assert.InRange(
            room.CreatedAt,
            beforeCreation,
            afterCreation);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenRoomNumberIsInvalid_ShouldThrowArgumentException(
        string roomNumber)
    {
        var action = () =>
            CreateValidRoom(roomNumber: roomNumber);

        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("roomNumber", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenPricePerNightIsInvalid_ShouldThrowArgumentException(
        int pricePerNight)
    {
        var action = () =>
            CreateValidRoom(pricePerNight: pricePerNight);

        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("pricePerNight", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenAdultsCapacityIsInvalid_ShouldThrowArgumentException(
        int adultsCapacity)
    {
        var action = () =>
            CreateValidRoom(adultsCapacity: adultsCapacity);

        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("adultsCapacity", exception.ParamName);
    }

    [Fact]
    public void Constructor_WhenChildCapacityIsNegative_ShouldThrowArgumentException()
    {
        var action = () =>
            CreateValidRoom(childCapacity: -1);

        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("childCapacity", exception.ParamName);
    }

    [Fact]
    public void Constructor_WhenChildCapacityIsZero_ShouldSucceed()
    {
        var room = CreateValidRoom(childCapacity: 0);

        Assert.Equal(0, room.ChildCapacity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenHotelIdIsInvalid_ShouldThrowArgumentException(
        int hotelId)
    {
        var action = () =>
            CreateValidRoom(hotelId: hotelId);

        var exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("hotelId", exception.ParamName);
    }

    [Fact]
    public void Constructor_ShouldInitializeCollectionsAsEmpty()
    {
        var room = CreateValidRoom();

        Assert.NotNull(room.RoomImages);
        Assert.NotNull(room.Bookings);
        Assert.NotNull(room.CartItems);

        Assert.Empty(room.RoomImages);
        Assert.Empty(room.Bookings);
        Assert.Empty(room.CartItems);
    }

    [Fact]
    public void Update_WhenDataIsValid_ShouldUpdateAllFields()
    {
        var room = CreateValidRoom();

        room.Update(
            roomNumber: "202",
            roomType: (RoomType)2,
            pricePerNight: 200m,
            adultsCapacity: 4,
            childCapacity: 2,
            hotelId: 20,
            description: "Updated room");

        Assert.Equal("202", room.RoomNumber);
        Assert.Equal((RoomType)2, room.RoomType);
        Assert.Equal(200m, room.PricePerNight);
        Assert.Equal(4, room.AdultsCapacity);
        Assert.Equal(2, room.ChildCapacity);
        Assert.Equal(20, room.HotelId);
        Assert.Equal("Updated room", room.Description);
    }

    [Fact]
    public void Update_WhenDataIsValid_ShouldSetUpdatedAt()
    {
        var room = CreateValidRoom();

        var beforeUpdate = DateTime.UtcNow;

        UpdateWithValidData(room);

        var afterUpdate = DateTime.UtcNow;

        Assert.NotNull(room.UpdatedAt);

        Assert.InRange(
            room.UpdatedAt!.Value,
            beforeUpdate,
            afterUpdate);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WhenRoomNumberIsInvalid_ShouldThrowArgumentException(
        string roomNumber)
    {
        var room = CreateValidRoom();

        var action = () =>
            UpdateWithValidData(
                room,
                roomNumber: roomNumber);

        Assert.Throws<ArgumentException>(action);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Update_WhenPricePerNightIsInvalid_ShouldThrowArgumentException(
        int pricePerNight)
    {
        var room = CreateValidRoom();

        var action = () =>
            UpdateWithValidData(
                room,
                pricePerNight: pricePerNight);

        Assert.Throws<ArgumentException>(action);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Update_WhenAdultsCapacityIsInvalid_ShouldThrowArgumentException(
        int adultsCapacity)
    {
        var room = CreateValidRoom();

        var action = () =>
            UpdateWithValidData(
                room,
                adultsCapacity: adultsCapacity);

        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Update_WhenChildCapacityIsNegative_ShouldThrowArgumentException()
    {
        var room = CreateValidRoom();

        var action = () =>
            UpdateWithValidData(
                room,
                childCapacity: -1);

        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Update_WhenChildCapacityIsZero_ShouldSucceed()
    {
        var room = CreateValidRoom();

        UpdateWithValidData(
            room,
            childCapacity: 0);

        Assert.Equal(0, room.ChildCapacity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Update_WhenHotelIdIsInvalid_ShouldThrowArgumentException(
        int hotelId)
    {
        var room = CreateValidRoom();

        var action = () =>
            UpdateWithValidData(
                room,
                hotelId: hotelId);

        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Update_ShouldNotChangeRoomStatuses()
    {
        var room = CreateValidRoom();

        room.ChangeStatus(false);
        room.ChangeOperationalAvailability(false);

        UpdateWithValidData(room);

        Assert.False(room.IsActive);
        Assert.False(room.IsOperationallyAvailable);
    }

    [Fact]
    public void Update_WhenValidationFails_ShouldNotChangeExistingValues()
    {
        var room = CreateValidRoom();

        var action = () =>
            UpdateWithValidData(
                room,
                roomNumber: "");

        Assert.Throws<ArgumentException>(action);

        Assert.Equal("101", room.RoomNumber);
        Assert.Equal(100m, room.PricePerNight);
        Assert.Equal(2, room.AdultsCapacity);
        Assert.Equal(1, room.ChildCapacity);
        Assert.Equal(10, room.HotelId);
        Assert.Equal("Test room", room.Description);
    }

    [Fact]
    public void ChangeStatus_WhenFalse_ShouldDeactivateRoom()
    {
        var room = CreateValidRoom();

        room.ChangeStatus(false);

        Assert.False(room.IsActive);
    }

    [Fact]
    public void ChangeStatus_WhenTrue_ShouldActivateRoom()
    {
        var room = CreateValidRoom();
        room.ChangeStatus(false);

        room.ChangeStatus(true);

        Assert.True(room.IsActive);
    }

    [Fact]
    public void ChangeStatus_ShouldSetUpdatedAt()
    {
        var room = CreateValidRoom();

        var beforeChange = DateTime.UtcNow;

        room.ChangeStatus(false);

        var afterChange = DateTime.UtcNow;

        Assert.NotNull(room.UpdatedAt);

        Assert.InRange(
            room.UpdatedAt!.Value,
            beforeChange,
            afterChange);
    }

    [Fact]
    public void ChangeOperationalAvailability_WhenFalse_ShouldMakeRoomUnavailable()
    {
        var room = CreateValidRoom();

        room.ChangeOperationalAvailability(false);

        Assert.False(room.IsOperationallyAvailable);
    }

    [Fact]
    public void ChangeOperationalAvailability_WhenTrue_ShouldMakeRoomAvailable()
    {
        var room = CreateValidRoom();
        room.ChangeOperationalAvailability(false);

        room.ChangeOperationalAvailability(true);

        Assert.True(room.IsOperationallyAvailable);
    }

    [Fact]
    public void ChangeOperationalAvailability_ShouldSetUpdatedAt()
    {
        var room = CreateValidRoom();

        var beforeChange = DateTime.UtcNow;

        room.ChangeOperationalAvailability(false);

        var afterChange = DateTime.UtcNow;

        Assert.NotNull(room.UpdatedAt);

        Assert.InRange(
            room.UpdatedAt!.Value,
            beforeChange,
            afterChange);
    }

    private static Room CreateValidRoom(
        string roomNumber = "101",
        RoomType roomType = (RoomType)1,
        decimal pricePerNight = 100m,
        int adultsCapacity = 2,
        int childCapacity = 1,
        int hotelId = 10,
        string? description = "Test room")
    {
        return new Room(
            roomNumber,
            roomType,
            pricePerNight,
            adultsCapacity,
            childCapacity,
            hotelId,
            description);
    }

    private static void UpdateWithValidData(
        Room room,
        string roomNumber = "202",
        RoomType roomType = (RoomType)2,
        decimal pricePerNight = 200m,
        int adultsCapacity = 4,
        int childCapacity = 2,
        int hotelId = 20,
        string? description = "Updated room")
    {
        room.Update(
            roomNumber,
            roomType,
            pricePerNight,
            adultsCapacity,
            childCapacity,
            hotelId,
            description);
    }
}