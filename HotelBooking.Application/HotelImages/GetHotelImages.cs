using HotelBooking.Application.Exceptions;
using HotelBooking.Application.HotelImages.Dtos;
using HotelBooking.Application.Interfaces;

namespace HotelBooking.Application.HotelImages;

public class GetHotelImages : IGetHotelImagesService
{
    private readonly IHotelRepository _hotelRepository;
    private readonly IHotelImageRepository  _hotelImageRepository;

    public GetHotelImages(IHotelRepository hotelRepository, IHotelImageRepository hotelImageRepository)
    {
        _hotelRepository = hotelRepository;
        _hotelImageRepository = hotelImageRepository;
    }
    
    public async Task<List<HotelImageResponseDto>> GetHotelImagesAsync(int hotelId)
    {
        var isActive = await _hotelRepository.IsActiveHotelAsync(hotelId);
        if (!isActive)
        {
            throw new NotFoundException("Hotel not found");
        }
        return await _hotelImageRepository.GetHotelImagesAsync(hotelId);
    }
}