using HotelBooking.Application.Common;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;

namespace HotelBooking.Application.Cities;

public class GetAllCities : IGetAllCitiesService
{
    private readonly ICityRepository _cityRepository;

    public GetAllCities(ICityRepository cityRepository)
    {
        _cityRepository = cityRepository;
    }

    public async Task<PagedResult<CityResponseDto>> GetAllCitiesAsync(CityListRequestDto request)
    {
        if (request.PageNumber < 1)
        {
            throw new BadRequestException("Page number must be greater than zero.");
        }

        return await _cityRepository.GetCitiesAsync(request);
    }
}
