using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Interfaces;

public interface IRoomImageRepository
{
    Task AddAsync(RoomImage roomImage);
    Task<RoomImage?> GetByIdAsync(int imageId);
    void Delete(RoomImage roomImage);
    Task SaveChangesAsync();
}