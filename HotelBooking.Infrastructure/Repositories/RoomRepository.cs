using HotelBooking.Application.Interfaces;
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

    public async Task<IEnumerable<Room>> GetRoomsAsync(string? search)
    {
        var searchQuery = _dbContext.Rooms.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            searchQuery = searchQuery.Where(r => r.RoomNumber.Contains(search));
        }
        return await searchQuery.ToListAsync();
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