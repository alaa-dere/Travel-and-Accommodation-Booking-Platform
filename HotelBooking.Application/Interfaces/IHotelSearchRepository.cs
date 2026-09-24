using HotelBooking.Application.Search;
using HotelBooking.Application.Search.Dtos;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application;

public interface IHotelSearchRepository
{
    Task<IEnumerable<HotelSearchResult>> GetCandidateHotelsAsync(HotelSearchRequestDto request);
}
    