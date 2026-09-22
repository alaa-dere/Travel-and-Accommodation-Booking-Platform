using HotelBooking.Application.Interfaces;

namespace HotelBooking.Application.Cities;

public class GetAllCities : IGetAllCitiesService
{
    private readonly ICityRepository _cityRepository;

    public GetAllCities(ICityRepository cityRepository)
    {
        _cityRepository = cityRepository;
    }

    public async Task<IEnumerable<CityResponseDto>> GetAllCitiesAsync(string? search)
    {
        var cities = await _cityRepository.GetCitiesAsync(search);
        var results = cities.Select(city => new CityResponseDto()
        {
            CityId =  city.CityId,
            Name = city.Name,
            Country =  city.Country,
            PostOffice =  city.PostOffice
        });
        return results;
    }
}