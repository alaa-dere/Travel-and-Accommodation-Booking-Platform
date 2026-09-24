using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Search.Dtos;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Search;

public class SearchHotels : ISearchHotelsService
{
    private readonly IHotelSearchRepository _hotelSearchRepository;

    public SearchHotels(IHotelSearchRepository hotelSearchRepository)
    {
        _hotelSearchRepository = hotelSearchRepository;
    }

    public async Task<IEnumerable<HotelSearchResponseDto>> SearchHotelsAsync(HotelSearchRequestDto request)
    {
        ValidateRequest(request);

        var candidateHotels = await _hotelSearchRepository.GetCandidateHotelsAsync(request.Destination, request.CheckIn, request.CheckOut, request.Rooms, request.Adults, request.Children);
        return candidateHotels.Select(MapToResponse);
    }

    private static void ValidateRequest(HotelSearchRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Destination))
        {
            throw new BadRequestException("Destination is required.");
        }

        if (request.CheckOut <= request.CheckIn)
        {
            throw new BadRequestException("Check-out date must be after the check-in date.");
        }

        if (request.Adults < 1)
        {
            throw new BadRequestException("Number of adults must be at least 1.");
        }

        if (request.Children < 0)
        {
            throw new BadRequestException("Number of children cannot be negative.");
        }

        if (request.Rooms < 1)
        {
            throw new BadRequestException("Number of rooms must be at least 1.");
        }
    }

    private static HotelSearchResponseDto MapToResponse(Hotel hotel)
    {
        return new HotelSearchResponseDto
        {
            HotelId = hotel.HotelId,
            Name = hotel.Name,
            City = hotel.City.Name,
            Address = hotel.Address,
            HotelType = hotel.HotelType,
            StartingPrice = hotel.Rooms.Min(room => room.PricePerNight)
        };
    }
}