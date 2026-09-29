using HotelBooking.Application.HotelImages.Dtos;

namespace HotelBooking.Application.HotelImages;

public interface IAddHotelImageService
{
    Task AddAsync(int hotelId, AddHotelImageRequestDto request);
}
