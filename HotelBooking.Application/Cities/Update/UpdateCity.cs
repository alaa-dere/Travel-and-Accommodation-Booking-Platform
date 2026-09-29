using HotelBooking.Application.Exceptions;
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
            throw new NotFoundException("City doesn't exist");
        }

        var normalizedName = request.Name.Trim();
        var normalizedCountry = request.Country.Trim();
        var normalizedPostOffice = request.PostOffice.Trim();

        if (await _cityRepository.ExistsAsync(normalizedName, normalizedCountry, normalizedPostOffice, id))
        {
            throw new ConflictException("A city with the same name, country, and post office already exists.");
        }

        city.Update(normalizedName, normalizedCountry, normalizedPostOffice, request.ThumbnailUrl);
        await _cityRepository.SaveChangesAsync();
        var hotelsCount = await _cityRepository.GetHotelsCountAsync(city.CityId);
        
        var response = new CityResponseDto
        {
            CityId = city.CityId,
            Name = city.Name,
            Country = city.Country,
            PostOffice = city.PostOffice,
            ThumbnailUrl = city.ThumbnailUrl,
            HotelsCount = hotelsCount,
            CreatedAt = city.CreatedAt,
            UpdatedAt = city.UpdatedAt
        };
        return response;
    }
}
