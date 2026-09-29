using HotelBooking.Application.AvailableRooms;
using HotelBooking.Application.AvailableRooms.Dtos;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Repositories;

public class AvailableRoomRepository : IAvailableRoomRepository
{
    private readonly HotelBookingDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public AvailableRoomRepository(HotelBookingDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<List<AvailableRoomResponseDto>> GetAvailableRoomsAsync(int hotelId, RoomAvailabilityCriteria criteria)
    {
        var rooms = CreateAvailableRoomsQuery(criteria)
            .Where(room => room.HotelId == hotelId);

        return await SelectAvailableRoom(rooms)
            .ToListAsync();
    }

    public async Task<AvailableRoomResponseDto?> GetAvailableRoomAsync(int hotelId, int roomId, RoomAvailabilityCriteria criteria)
    {
        var rooms = CreateAvailableRoomsQuery(criteria)
            .Where(room => room.HotelId == hotelId && room.RoomId == roomId);

        return await SelectAvailableRoom(rooms)
            .FirstOrDefaultAsync();
    }

    public async Task<AvailableRoomResponseDto?> GetAvailableRoomAsync(int roomId, RoomAvailabilityCriteria criteria)
    {
        var rooms = CreateAvailableRoomsQuery(criteria)
            .Where(room => room.RoomId == roomId);

        return await SelectAvailableRoom(rooms)
            .FirstOrDefaultAsync();
    }

    private IQueryable<Room> CreateAvailableRoomsQuery(RoomAvailabilityCriteria criteria)
    {
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

        return _dbContext.Rooms
            .AsNoTracking()
            .Where(room =>
                room.IsActive &&
                room.IsOperationallyAvailable &&
                room.Hotel!.IsActive &&
                room.AdultsCapacity >= criteria.Adults &&
                room.ChildCapacity >= criteria.Children &&
                !room.Bookings.Any(booking =>
                    booking.BookingStatus != BookingStatus.Cancelled &&
                    (booking.BookingStatus != BookingStatus.Pending || booking.PendingExpiresAt > utcNow) &&
                    booking.CheckIn < criteria.CheckOut &&
                    booking.CheckOut > criteria.CheckIn));
    }

    private static IQueryable<AvailableRoomResponseDto> SelectAvailableRoom(IQueryable<Room> rooms)
    {
        return rooms.Select(room => new AvailableRoomResponseDto
        {
            RoomId = room.RoomId,
            HotelId = room.HotelId,
            RoomType = room.RoomType,
            Description = room.Description,
            AdultsCapacity = room.AdultsCapacity,
            ChildCapacity = room.ChildCapacity,
            PricePerNight = room.PricePerNight,
            Images = room.RoomImages
                .OrderBy(image => image.DisplayOrder)
                .Select(image => new RoomImageResponseDto
                {
                    ImageUrl = image.ImageUrl,
                    DisplayOrder = image.DisplayOrder
                })
                .ToList()
        });
    }
}
