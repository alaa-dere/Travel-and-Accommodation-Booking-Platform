using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Hotels.Dtos;
using HotelBooking.Application.Interfaces;

namespace HotelBooking.Application.Hotels.Update;

public class UpdateHotel : IUpdateHotelService
{
    private readonly IHotelRepository _hotelRepository;
    private readonly ICityRepository _cityRepository;

    public UpdateHotel(IHotelRepository hotelRepository,  ICityRepository cityRepository)
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
        
        hotel.Update(request.Name, request.OwnerName, request.Address, request.Latitude, request.Longitude, request.HotelType, request.CityId,request.Description,request.History);
        await _hotelRepository.SaveChangesAsync();
        
        var response = new HotelResponseDto
            {HotelId = hotel.HotelId,
                CityId = hotel.CityId,
                Name = hotel.Name,
                OwnerName =  hotel.OwnerName,
                Address = hotel.Address,  
                Latitude = hotel.Latitude,
                Longitude = hotel.Longitude, 
                HotelType = hotel.HotelType,
                Description = hotel.Description,
                History = hotel.History,
                IsActive = hotel.IsActive
            };
        return response;
    }
}