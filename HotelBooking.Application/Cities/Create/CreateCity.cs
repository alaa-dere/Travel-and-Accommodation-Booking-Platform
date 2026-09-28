using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Cities;

public class CreateCity : ICreateCityService
{
    private readonly ICityRepository _cityRepository;

    public CreateCity(ICityRepository cityRepository)
    {
        _cityRepository = cityRepository;
    }
    
    public async Task<CityResponseDto> CreateCityAsync(CityRequestDto request)
    {
        var city = new City(request.Name, request.Country, request.PostOffice, request.ThumbnailUrl);
        _cityRepository.Add(city);
        await _cityRepository.SaveChangesAsync();
        
        var response = new CityResponseDto
        {
            CityId = city.CityId,
            Name = city.Name,
            Country = city.Country,
            PostOffice = city.PostOffice,
            ThumbnailUrl = city.ThumbnailUrl
        };
        return response;
    }
}
