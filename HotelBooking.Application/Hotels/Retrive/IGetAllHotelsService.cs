using HotelBooking.Application.Common;
using HotelBooking.Application.Hotels.Dtos;

namespace HotelBooking.Application.Hotels.Retrive;

public interface IGetAllHotelsService
{
    Task<PagedResult<HotelResponseDto>> GetAllHotelsAsync(HotelListRequestDto request);
}
