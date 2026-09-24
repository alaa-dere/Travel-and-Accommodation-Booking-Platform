using HotelBooking.Application.Common;
using HotelBooking.Application.Search;
using HotelBooking.Application.Search.Dtos;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application;

public interface IHotelSearchRepository
{
    Task<PagedResult<HotelSearchResult>> GetCandidateHotelsAsync(HotelSearchRequestDto request);
    
}
    