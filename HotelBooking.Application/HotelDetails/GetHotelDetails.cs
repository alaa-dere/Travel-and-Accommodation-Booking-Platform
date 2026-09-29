using HotelBooking.Application.Exceptions;
using HotelBooking.Application.HotelDetails.Dtos;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.RecentlyVisitedHotels;

namespace HotelBooking.Application.HotelDetails;

public class GetHotelDetails : IGetHotelDetailsService
{
    private readonly IHotelRepository _hotelRepository;
    private readonly IRecordHotelVisitService _recordHotelVisitService;

    public GetHotelDetails(IHotelRepository hotelRepository, IRecordHotelVisitService recordHotelVisitService)
    {
        _hotelRepository = hotelRepository;
        _recordHotelVisitService = recordHotelVisitService;
    }

    public async Task<HotelDetailsResponseDto> GetHotelDetailsAsync(int hotelId, int userId)
    {
        var result = await _hotelRepository.GetHotelDetailsAsync(hotelId);
        if (result == null)
        {
            throw new NotFoundException($"Hotel with id {hotelId} not found");
        }
        await _recordHotelVisitService.RecordVisitAsync(userId, hotelId);
        return result;
    }
}