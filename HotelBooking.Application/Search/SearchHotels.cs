using HotelBooking.Application.Common;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Search.Dtos;

namespace HotelBooking.Application.Search;

public class SearchHotels : ISearchHotelsService
{
    private readonly IHotelSearchRepository _hotelSearchRepository;

    public SearchHotels(IHotelSearchRepository hotelSearchRepository)
    {
        _hotelSearchRepository = hotelSearchRepository;
    }

    public async Task<PagedResult<HotelSearchResponseDto>> SearchHotelsAsync(HotelSearchRequestDto request)
    {
        ValidateRequest(request);

        var candidateHotels = await _hotelSearchRepository.GetCandidateHotelsAsync(request);
        return new PagedResult<HotelSearchResponseDto>
        {
            Items = candidateHotels.Items.Select(MapToResponse),
            PageNumber = candidateHotels.PageNumber,
            HasNextPage = candidateHotels.HasNextPage
        };
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
        
        if (request.MinPrice.HasValue && request.MinPrice.Value < 0)
        {
            throw new BadRequestException("Min price cannot be negative.");
        }

        if (request.MaxPrice.HasValue && request.MaxPrice.Value < 0)
        {
            throw new BadRequestException("Max price cannot be negative.");
        }

        if (request.MinPrice.HasValue && request.MaxPrice.HasValue && request.MinPrice.Value > request.MaxPrice.Value)
        {
            throw new BadRequestException("Min price cannot be greater than max price.");
        }

        if (request.MinRating.HasValue && (request.MinRating.Value < 1 || request.MinRating.Value > 5))
        {
            throw new BadRequestException("Min rating must be between 1 and 5.");
        }

        if (request.AmenityIds != null && request.AmenityIds.Any(id => id <= 0))
        {
            throw new BadRequestException("Amenity IDs must be greater than zero.");
        }
        
        if(request.PageNumber < 1)
        {
            throw new BadRequestException("PageNumber must be greater than zero.");
        }
        
    }

    private static HotelSearchResponseDto MapToResponse(HotelSearchResult result)
    {
        var hotel = result.Hotel;
        return new HotelSearchResponseDto
        {
            HotelId = hotel.HotelId,
            Name = hotel.Name,
            City = hotel.City.Name,
            Address = hotel.Address,
            HotelType = hotel.HotelType,
            StartingPrice = hotel.Rooms.Min(room => room.PricePerNight),
            Rating = result.Rating,
            ThumbnailUrl = result.ThumbnailUrl,
            BriefDescription = hotel.Description
        };
    }
}