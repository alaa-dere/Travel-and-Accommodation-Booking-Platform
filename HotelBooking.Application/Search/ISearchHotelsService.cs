using HotelBooking.Application.Common;
using HotelBooking.Application.Search.Dtos;

namespace HotelBooking.Application.Search;

public interface ISearchHotelsService
{
    Task<PagedResult<HotelSearchResponseDto>> SearchHotelsAsync(HotelSearchRequestDto request);
}
