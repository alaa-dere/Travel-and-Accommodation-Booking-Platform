using HotelBooking.Application.Exceptions;
using HotelBooking.Application.HotelLocations.Dtos;
using HotelBooking.Application.Interfaces;

namespace HotelBooking.Application.HotelLocations;

public class GetHotelLocationService : IGetHotelLocationService
{
    private readonly IHotelLocationRepository _hotelLocationRepository;

    public GetHotelLocationService(IHotelLocationRepository hotelLocationRepository)
    {
        _hotelLocationRepository = hotelLocationRepository;
    }

    public async Task<HotelLocationResponseDto> GetHotelLocationAsync(int hotelId)
    {
        var result = await _hotelLocationRepository.GetHotelLocationAsync(hotelId);
        if (result == null)
        {
            throw new NotFoundException("Hotel Not Found");
        }
        return result;
    }
}