using HotelBooking.Application.Common;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Hotels.Dtos;
using HotelBooking.Application.Interfaces;

namespace HotelBooking.Application.Hotels.Retrive;

public class GetAllHotels : IGetAllHotelsService
{
    private readonly IHotelRepository _hotelRepository;

    public GetAllHotels(IHotelRepository hotelRepository)
    {
        _hotelRepository = hotelRepository;
    }

    public async Task<PagedResult<HotelResponseDto>> GetAllHotelsAsync(HotelListRequestDto request)
    {
        if (request.PageNumber < 1)
        {
            throw new BadRequestException("Page number must be greater than zero.");
        }

        return await _hotelRepository.GetHotelsAsync(request);
    }
}
