using HotelBooking.Application.Cities;
using HotelBooking.Application.Common;

namespace HotelBooking.Application.Interfaces;

public interface IGetAllCitiesService
{
    Task<PagedResult<CityResponseDto>> GetAllCitiesAsync(CityListRequestDto request);
}
