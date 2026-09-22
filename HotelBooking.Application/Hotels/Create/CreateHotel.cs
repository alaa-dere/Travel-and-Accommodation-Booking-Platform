using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Hotels.Dtos;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Hotels.Create;

public class CreateHotel : ICreateHotelService
{
    private readonly IHotelRepository _hotelRepository;
    private readonly ICityRepository _cityRepository;

    public CreateHotel(IHotelRepository hotelRepository,  ICityRepository cityRepository)
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
        
        var hotel = new Hotel (request.Name, request.OwnerName,request.Address, request.Latitude, request.Longitude, request.HotelType, request.CityId);
        _hotelRepository.Add(hotel);
        await _hotelRepository.SaveChangesAsync();
        
        var response = new HotelResponseDto{HotelId = hotel.HotelId, CityId =  hotel.CityId, Name = hotel.Name,  OwnerName = hotel.OwnerName,  HotelType = hotel.HotelType, Address =  hotel.Address, Latitude = hotel.Latitude, Longitude = hotel.Longitude};
        return response;
    }
}