using HotelBooking.Application.Exceptions;
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

        if (await _cityRepository.ExistsAsync(city.Name, city.Country, city.PostOffice))
        {
            throw new ConflictException("A city with the same name, country, and post office already exists.");
        }

        _cityRepository.Add(city);
        await _cityRepository.SaveChangesAsync();
        
        var response = new CityResponseDto
        {
            CityId = city.CityId,
            Name = city.Name,
            Country = city.Country,
            PostOffice = city.PostOffice,
            ThumbnailUrl = city.ThumbnailUrl,
            HotelsCount = 0,
            CreatedAt = city.CreatedAt,
            UpdatedAt = city.UpdatedAt
        };
        return response;
    }
}
