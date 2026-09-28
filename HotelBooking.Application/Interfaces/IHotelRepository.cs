using HotelBooking.Application.HotelDetails.Dtos;
using HotelBooking.Application.Common;
using HotelBooking.Application.Hotels.Dtos;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Interfaces;

public interface IHotelRepository
{
    Task<PagedResult<HotelResponseDto>> GetHotelsAsync(HotelListRequestDto request);
    Task<HotelResponseDto?> GetHotelResponseAsync(int hotelId);
    void Add(Hotel hotel);
    Task SaveChangesAsync();
    Task<Hotel?> GetHotelByIdAsync(int id);
    Task<HotelDetailsResponseDto?> GetHotelDetailsAsync(int id);
    Task<bool> IsActiveHotelAsync(int hotelId);
    Task<bool> ExistsAsync(string name, int cityId, string address, int? excludedHotelId = null);
}
