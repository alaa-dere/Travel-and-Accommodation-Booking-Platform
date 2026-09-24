using HotelBooking.Application.Exceptions;
using HotelBooking.Application.HotelDetails.Dtos;
using HotelBooking.Application.Interfaces;

namespace HotelBooking.Application.HotelDetails;

public class GetHotelDetails : IGetHotelDetailsService
{
    private readonly IHotelRepository _hotelRepository;

    public GetHotelDetails(IHotelRepository hotelRepository)
    {
        _hotelRepository = hotelRepository;
    }
    
    public async Task<HotelDetailsResponseDto> GetHotelDetailsAsync(int id)
    {
        var result = await _hotelRepository.GetHotelDetailsAsync(id);
        if (result == null)
        {
            throw new NotFoundException($"Hotel with id {id} not found");
        }
        
        return result;
    }
}