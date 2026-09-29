using HotelBooking.Application.HotelLocations.Dtos;

namespace HotelBooking.Application.HotelLocations;

public interface IGetHotelLocationService
{
    Task<HotelLocationResponseDto> GetHotelLocationAsync(int hotelId);
}