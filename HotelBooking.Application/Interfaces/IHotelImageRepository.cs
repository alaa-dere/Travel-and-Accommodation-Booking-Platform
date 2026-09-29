using HotelBooking.Application.HotelImages.Dtos;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Interfaces;

public interface IHotelImageRepository
{
    Task<List<HotelImageResponseDto>> GetHotelImagesAsync(int hotelId);
    Task<HotelImage?> GetByIdAsync(int imageId);
    Task<bool> ExistsAsync(int hotelId, string imageUrl, int displayOrder);
    Task AddAsync(HotelImage image);
    void Delete(HotelImage image);
    Task SaveChangesAsync();
}
