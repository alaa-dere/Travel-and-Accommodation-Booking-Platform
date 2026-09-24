using HotelBooking.Application.HotelImages.Dtos;

namespace HotelBooking.Application.Interfaces;

public interface IHotelImageRepository
{
    Task<List<HotelImageResponseDto>> GetHotelImagesAsync(int hotelId);
}