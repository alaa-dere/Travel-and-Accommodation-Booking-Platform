using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Repositories;

public class HotelRepository : IHotelRepository
{
    private readonly HotelBookingDbContext _dbContext;
    public HotelRepository(HotelBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<Hotel>> GetHotelsAsync(string? search)
    {
        var searchQuery = _dbContext.Hotels.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
           // searchQuery = searchQuery.Where(c => c.Name.Contains(search) || c.Country.Contains(search));
        }
        return await searchQuery.ToListAsync();
    }

    public void Add(Hotel hotel)
    {
        _dbContext.Hotels.Add(hotel);
    }

    public Task<Hotel?> GetHotelByIdAsync(int id)
    {
        var hotel = _dbContext.Hotels.FirstOrDefaultAsync(c => c.HotelId == id);
        return hotel;
    }
    
    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
    
    public void DeleteHotel(Hotel hotel){
        _dbContext.Hotels.Remove(hotel);
    }
}