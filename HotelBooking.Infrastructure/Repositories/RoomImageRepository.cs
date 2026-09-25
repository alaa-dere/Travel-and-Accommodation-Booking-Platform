using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Repositories;

public class RoomImageRepository : IRoomImageRepository
{
    private readonly HotelBookingDbContext _dbContext;

    public RoomImageRepository(HotelBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(RoomImage roomImage)
    {
        await _dbContext.RoomImages.AddAsync(roomImage);
    }

    public Task<RoomImage?> GetByIdAsync(int imageId)
    {
        return _dbContext.RoomImages.FirstOrDefaultAsync(image => image.RoomImageId == imageId);
    }

    public void Delete(RoomImage roomImage)
    {
        _dbContext.RoomImages.Remove(roomImage);
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}