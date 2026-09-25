using HotelBooking.Application.HotelLocations.Dtos;

namespace HotelBooking.Application.Interfaces;

public interface IHotelLocationRepository
{
    Task<HotelLocationResponseDto?> GetHotelLocationAsync(int hotelId);
}