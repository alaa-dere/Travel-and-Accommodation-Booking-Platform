namespace HotelBooking.Application.Search.Dtos;

public interface ISearchHotelsService
{
    Task<IEnumerable<HotelSearchResponseDto>> SearchHotelsAsync(HotelSearchRequestDto request);
}