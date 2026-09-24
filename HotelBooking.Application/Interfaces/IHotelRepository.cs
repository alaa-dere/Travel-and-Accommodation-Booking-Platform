using HotelBooking.Application.HotelDetails.Dtos;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Interfaces;

public interface IHotelRepository
{
    Task<IEnumerable<Hotel>> GetHotelsAsync(string? search);
    void Add(Hotel hotel);
    Task SaveChangesAsync();
    Task<Hotel?> GetHotelByIdAsync(int id);
    void DeleteHotel(Hotel hotel);
    Task<HotelDetailsResponseDto?> GetHotelDetailsAsync(int id);
    Task<bool> IsActiveHotelAsync(int hotelId);
}