using HotelBooking.Application.RecentlyVisitedHotels.Dtos;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Interfaces;

public interface IRecentlyVisitedHotelRepository
{
    Task<RecentlyVisitedHotel?> GetVisitAsync(int userId, int hotelId);
    Task AddAsync(RecentlyVisitedHotel visit);
    Task SaveChangesAsync();
    Task<List<RecentlyVisitedHotelResponseDto>> GetRecentlyVisitedHotelsAsync(int userId);
}