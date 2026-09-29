using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Hotels.Dtos;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Hotels.Create;

public class CreateHotel : ICreateHotelService
{
    private readonly IHotelRepository _hotelRepository;
    private readonly ICityRepository _cityRepository;

    public CreateHotel(IHotelRepository hotelRepository, ICityRepository cityRepository)
    {
        _hotelRepository = hotelRepository;
        _cityRepository = cityRepository;
    }
    
    public async Task<HotelResponseDto> CreateHotelAsync(HotelRequestDto request)
    {
        var city = await _cityRepository.GetCityByIdAsync(request.CityId);
        if (city == null)
        {
            throw new NotFoundException("City does not exist");
        }
        
        var hotel = new Hotel(
            request.Name,
            request.OwnerName,
            request.Address,
            request.Latitude,
            request.Longitude,
            request.HotelType,
            request.CityId,
            request.Description,
            request.History);

        if (await _hotelRepository.ExistsAsync(hotel.Name, hotel.CityId, hotel.Address))
        {
            throw new ConflictException("A hotel with the same name, city, and address already exists.");
        }

        _hotelRepository.Add(hotel);
        await _hotelRepository.SaveChangesAsync();

        return await _hotelRepository.GetHotelResponseAsync(hotel.HotelId)
               ?? throw new InvalidOperationException("The created hotel could not be retrieved.");
    }
}
