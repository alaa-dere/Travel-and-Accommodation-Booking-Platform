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

    public async Task<List<AvailableRoomResponseDto>> GetAvailableRoomsAsync(int hotelId, DateTime checkIn, DateTime checkOut, int adults, int children)
    {
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

        return await _dbContext.Rooms.AsNoTracking()
            .Where(room => room.HotelId == hotelId && room.IsActive && room.IsOperationallyAvailable &&
                           room.AdultsCapacity >= adults && room.ChildCapacity >= children &&
                           !room.Bookings.Any(booking => booking.BookingStatus != BookingStatus.Cancelled &&
                                                         (booking.BookingStatus != BookingStatus.Pending || booking.PendingExpiresAt > utcNow) &&
                                                         booking.CheckIn < checkOut && booking.CheckOut > checkIn))
            .Select(room => new AvailableRoomResponseDto
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
                    }).ToList()
            }).ToListAsync();
    }
    
    public async Task<AvailableRoomResponseDto?> GetAvailableRoomAsync(int hotelId,int roomId, DateTime checkIn, DateTime checkOut, int adults, int children)
    {
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

        return await _dbContext.Rooms.AsNoTracking()
            .Where(room => room.RoomId == roomId && room.IsActive && room.IsOperationallyAvailable && room.HotelId == hotelId &&
                           room.AdultsCapacity >= adults && room.ChildCapacity >= children && room.Hotel!.IsActive &&
                           !room.Bookings.Any(booking => booking.BookingStatus != BookingStatus.Cancelled &&
                                                         (booking.BookingStatus != BookingStatus.Pending || booking.PendingExpiresAt > utcNow) &&
                                                         booking.CheckIn < checkOut && booking.CheckOut > checkIn))
            .Select(room => new AvailableRoomResponseDto
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
                    }).ToList()
            }).FirstOrDefaultAsync();
    }
    
    public async Task<AvailableRoomResponseDto?> GetAvailableRoomAsync(
        int roomId,
        DateTime checkIn,
        DateTime checkOut,
        int adults,
        int children)
    {
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

        return await _dbContext.Rooms.AsNoTracking()
            .Where(room => room.RoomId == roomId && room.IsActive && room.IsOperationallyAvailable && 
                           room.Hotel!.IsActive && room.AdultsCapacity >= adults && room.ChildCapacity >= children &&
                           !room.Bookings.Any(booking => booking.BookingStatus != BookingStatus.Cancelled &&
                                                         (booking.BookingStatus != BookingStatus.Pending || booking.PendingExpiresAt > utcNow) &&
                                                         booking.CheckIn < checkOut && booking.CheckOut > checkIn))
            .Select(room => new AvailableRoomResponseDto
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
                    }).ToList()
            }).FirstOrDefaultAsync();
    }
}
