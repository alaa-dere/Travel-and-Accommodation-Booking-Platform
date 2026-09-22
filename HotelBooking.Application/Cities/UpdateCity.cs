using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Cities;

public class UpdateCity : IUpdateCityService
{
    private readonly ICityRepository _cityRepository;

    public UpdateCity(ICityRepository cityRepository)
    {
        _cityRepository = cityRepository;
    }
    
    public async Task<CityResponseDto> UpdateCityAsync(int id, CityRequestDto request)
    {
        var city = await _cityRepository.GetCityByIdAsync(id);
        if (city == null)
        {
            throw new ArgumentException("City doesn't exist");
        }
        
        city.Update(request.Name, request.Country, request.PostOffice);
        await _cityRepository.SaveChangesAsync();
        
        var response = new CityResponseDto{CityId = city.CityId, Name = city.Name, Country = city.Country, PostOffice =  city.PostOffice };
        return response;
    }
}
