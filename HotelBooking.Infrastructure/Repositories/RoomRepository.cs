using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Rooms.Dtos;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Repositories;

public class RoomRepository : IRoomRepository
{
    private readonly HotelBookingDbContext _dbContext;
    public RoomRepository(HotelBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<Room>> GetRoomsAsync(RoomFilterDto filter)
    {
        var query = _dbContext.Rooms.Include(room => room.RoomImages).AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            query = query.Where(room => room.RoomNumber.Contains(filter.Search));
        }

        if (filter.HotelId.HasValue)
        {
            query = query.Where(room => room.HotelId == filter.HotelId.Value);
        }

        if (filter.RoomType.HasValue)
        {
            query = query.Where(room => room.RoomType == filter.RoomType.Value);
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(room => room.IsActive == filter.IsActive.Value);
        }

        if (filter.IsOperationallyAvailable.HasValue)
        {
            query = query.Where(room => room.IsOperationallyAvailable == filter.IsOperationallyAvailable.Value);
        }

        return await query.ToListAsync();
    }

    public void Add(Room room)
    {
        _dbContext.Rooms.Add(room);
    }

    public Task<Room?> GetRoomByIdAsync(int id)
    {
        var room = _dbContext.Rooms.FirstOrDefaultAsync(r => r.RoomId == id);
        return room;
    }
    
    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
    
    public void DeleteRoom(Room room){
        _dbContext.Rooms.Remove(room);
    }
}