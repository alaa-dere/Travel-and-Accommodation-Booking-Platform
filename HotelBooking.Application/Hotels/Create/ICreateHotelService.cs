using HotelBooking.Application.Hotels.Dtos;

namespace HotelBooking.Application.Hotels.Create;

public interface ICreateHotelService
{
    Task<HotelResponseDto> CreateHotelAsync(HotelRequestDto request);
}