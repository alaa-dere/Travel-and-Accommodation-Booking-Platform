using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Hotels.Dtos;
using HotelBooking.Application.Interfaces;

namespace HotelBooking.Application.Hotels.Update;

public class UpdateHotel : IUpdateHotelService
{
    private readonly IHotelRepository _hotelRepository;
    private readonly ICityRepository _cityRepository;

    public UpdateHotel(IHotelRepository hotelRepository, ICityRepository cityRepository)
    {
        _hotelRepository = hotelRepository;
        _cityRepository = cityRepository;
    }
    
    public async Task<HotelResponseDto> UpdateHotelAsync(int id, HotelRequestDto request)
    {
        var hotel = await _hotelRepository.GetHotelByIdAsync(id);
        if (hotel == null)
        {
            throw new NotFoundException("Hotel doesn't exist");
        }
        
        var city = await _cityRepository.GetCityByIdAsync(request.CityId);
        if (city == null)
        {
            throw new NotFoundException("City does not exist");
        }

        var normalizedName = request.Name.Trim();
        var normalizedAddress = request.Address.Trim();
        if (await _hotelRepository.ExistsAsync(normalizedName, request.CityId, normalizedAddress, id))
        {
            throw new ConflictException("A hotel with the same name, city, and address already exists.");
        }

        hotel.Update(
            normalizedName,
            request.OwnerName,
            normalizedAddress,
            request.Latitude,
            request.Longitude,
            request.HotelType,
            request.CityId,
            request.Description,
            request.History);
        await _hotelRepository.SaveChangesAsync();

        return await _hotelRepository.GetHotelResponseAsync(hotel.HotelId)
               ?? throw new InvalidOperationException("The updated hotel could not be retrieved.");
    }
}
