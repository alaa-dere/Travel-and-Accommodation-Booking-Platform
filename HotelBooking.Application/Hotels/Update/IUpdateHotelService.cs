using HotelBooking.Application.Hotels.Dtos;

namespace HotelBooking.Application.Hotels.Update;

public interface IUpdateHotelService
{
    Task<HotelResponseDto> UpdateHotelAsync(int id, HotelRequestDto request);
}