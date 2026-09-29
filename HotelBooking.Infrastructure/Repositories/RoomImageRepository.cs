using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Exceptions;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

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

    public Task<bool> ExistsAsync(int roomId, string imageUrl, int displayOrder)
    {
        return _dbContext.RoomImages.AsNoTracking().AnyAsync(image => image.RoomId == roomId && (image.ImageUrl == imageUrl || image.DisplayOrder == displayOrder));
    }

    public void Delete(RoomImage roomImage)
    {
        _dbContext.RoomImages.Remove(roomImage);
    }

    public async Task SaveChangesAsync()
    {
        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new ConflictException("The image URL or display order already exists for this room.");
        }
    }
}
