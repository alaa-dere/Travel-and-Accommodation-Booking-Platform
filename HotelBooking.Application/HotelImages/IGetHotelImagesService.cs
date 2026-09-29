using HotelBooking.Application.HotelImages.Dtos;

namespace HotelBooking.Application.HotelImages;

public interface IGetHotelImagesService
{
    Task<List<HotelImageResponseDto>> GetHotelImagesAsync(int hotelId);
}