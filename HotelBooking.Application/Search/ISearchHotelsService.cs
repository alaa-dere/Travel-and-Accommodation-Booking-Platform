using HotelBooking.Application.Common;

namespace HotelBooking.Application.Search.Dtos;

public interface ISearchHotelsService
{
    Task<PagedResult<HotelSearchResponseDto>> SearchHotelsAsync(HotelSearchRequestDto request);
}