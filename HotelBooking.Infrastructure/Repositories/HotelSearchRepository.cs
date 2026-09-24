using HotelBooking.Application;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Repositories;

public class HotelSearchRepository : IHotelSearchRepository
{
    private readonly HotelBookingDbContext _dbContext;

    public HotelSearchRepository(HotelBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<Hotel>> GetCandidateHotelsAsync(string destination, DateTime checkIn, DateTime checkOut, int requiredRooms, int adults = 1, int children = 0)
    {
        var searchTerm = $"%{destination.Trim()}%";
        var query = _dbContext.Hotels.AsNoTracking()
            .Where(hotel => hotel.IsActive)
            .Where(hotel => 
                EF.Functions.Like(hotel.Name, searchTerm) ||
                EF.Functions.Like(hotel.City.Name, searchTerm))
            .Where(hotel => hotel.Rooms.Count(room =>
                room.IsActive &&
                room.IsOperationallyAvailable &&
                room.AdultsCapacity >= adults &&
                room.ChildCapacity >= children &&
                !room.Bookings.Any(booking =>
                    booking.BookingStatus != BookingStatus.Cancelled &&
                    booking.CheckIn < checkOut &&
                    booking.CheckOut > checkIn
                )
            ) >= requiredRooms)
            .Include(hotel => hotel.City)
            .Include(hotel => hotel.Rooms.Where(room =>
                room.IsActive &&
                room.IsOperationallyAvailable &&
                room.AdultsCapacity >= adults &&
                room.ChildCapacity >= children &&
                !room.Bookings.Any(booking =>
                    booking.BookingStatus != BookingStatus.Cancelled &&
                    booking.CheckIn < checkOut &&
                    booking.CheckOut > checkIn
                )));

        return await query.ToListAsync();
    }
}