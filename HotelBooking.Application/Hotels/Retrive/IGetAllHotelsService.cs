using HotelBooking.Application.Hotels.Dtos;

namespace HotelBooking.Application.Hotels.Retrive;

public interface IGetAllHotelsService
{
    Task<IEnumerable<HotelResponseDto>> GetAllHotelsAsync(string? search);

}