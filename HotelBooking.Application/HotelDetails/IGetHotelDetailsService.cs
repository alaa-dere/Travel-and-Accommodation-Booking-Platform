using HotelBooking.Application.HotelDetails.Dtos;

namespace HotelBooking.Application.HotelDetails;

public interface IGetHotelDetailsService
{
    Task<HotelDetailsResponseDto> GetHotelDetailsAsync(int hotelId, int userId);
}