using HotelBooking.Application.HotelImages.Dtos;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace HotelBooking.Infrastructure.Repositories;

public class HotelImageRepository : IHotelImageRepository
{
    private readonly HotelBookingDbContext _dbContext;
    public HotelImageRepository(HotelBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<HotelImageResponseDto>> GetHotelImagesAsync(int hotelId)
    {
             return _dbContext.HotelImages.AsNoTracking()
            .Where(image => image.HotelId == hotelId && image.Hotel!.IsActive)
            .OrderBy(image => image.DisplayOrder)
            .Select(image => new HotelImageResponseDto
            {
                ImageUrl = image.ImageUrl,
                DisplayOrder = image.DisplayOrder
            }).ToListAsync();
    }

    public Task<HotelImage?> GetByIdAsync(int imageId) => _dbContext.HotelImages.FirstOrDefaultAsync(image => image.HotelImageId == imageId);

    public Task<bool> ExistsAsync(int hotelId, string imageUrl, int displayOrder) =>
        _dbContext.HotelImages.AsNoTracking().AnyAsync(image => image.HotelId == hotelId && (image.ImageUrl == imageUrl || image.DisplayOrder == displayOrder));
    public async Task AddAsync(HotelImage image) => await _dbContext.HotelImages.AddAsync(image);

    public void Delete(HotelImage image) => _dbContext.HotelImages.Remove(image);

    public async Task SaveChangesAsync()
    {
        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new ConflictException("The image URL or display order already exists for this hotel.");
        }
    }
}
