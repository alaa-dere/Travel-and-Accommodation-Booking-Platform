using HotelBooking.Application.HotelImages.Dtos;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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
}