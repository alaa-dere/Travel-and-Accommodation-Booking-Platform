using HotelBooking.Application.AvailableRooms.Dtos;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Repositories;

public class AvailableRoomRepository : IAvailableRoomRepository
{
    private readonly HotelBookingDbContext _dbContext;

    public AvailableRoomRepository(HotelBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<AvailableRoomResponseDto>> GetAvailableRoomsAsync(int hotelId, DateTime checkIn, DateTime checkOut, int adults, int children)
    {
        return await _dbContext.Rooms.AsNoTracking()
            .Where(room => room.HotelId == hotelId && room.IsActive && room.IsOperationallyAvailable &&
                           room.AdultsCapacity >= adults && room.ChildCapacity >= children &&
                           !room.Bookings.Any(booking => booking.BookingStatus != BookingStatus.Cancelled && booking.CheckIn < checkOut && booking.CheckOut > checkIn))
            .Select(room => new AvailableRoomResponseDto
            {
                RoomId = room.RoomId,
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
        return await _dbContext.Rooms.AsNoTracking()
            .Where(room => room.RoomId == roomId && room.IsActive && room.IsOperationallyAvailable && room.HotelId == hotelId &&
                           room.AdultsCapacity >= adults && room.ChildCapacity >= children && room.Hotel!.IsActive &&
                           !room.Bookings.Any(booking => booking.BookingStatus != BookingStatus.Cancelled && booking.CheckIn < checkOut && booking.CheckOut > checkIn))
            .Select(room => new AvailableRoomResponseDto
            {
                RoomId = room.RoomId,
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