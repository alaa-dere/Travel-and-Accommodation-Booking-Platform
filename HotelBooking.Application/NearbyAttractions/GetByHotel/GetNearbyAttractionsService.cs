using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.NearbyAttractions.Dtos;

namespace HotelBooking.Application.NearbyAttractions.GetByHotel;

public class GetNearbyAttractionsService : IGetNearbyAttractionsService
{
    private readonly INearbyAttractionRepository _attractionRepository;
    private readonly IHotelRepository _hotelRepository;

    public GetNearbyAttractionsService(INearbyAttractionRepository attractionRepository, IHotelRepository hotelRepository)
    {
        _attractionRepository = attractionRepository;
        _hotelRepository = hotelRepository;
    }

    public async Task<IEnumerable<NearbyAttractionResponseDto>> GetByHotelIdAsync(int hotelId)
    {
        if (hotelId <= 0)
        {
            throw new BadRequestException("Invalid hotel ID.");
        }

        var hotel = await _hotelRepository.GetHotelByIdAsync(hotelId);

        if (hotel == null)
        {
            throw new NotFoundException("Hotel not found.");
        }

        var attractions = await _attractionRepository.GetByHotelIdAsync(hotelId);

        return attractions.Select(attraction =>
            new NearbyAttractionResponseDto
            {
                NearbyAttractionId = attraction.NearbyAttractionId,
                HotelId = attraction.HotelId,
                Name = attraction.Name,
                Description = attraction.Description,
                Latitude = attraction.Latitude,
                Longitude = attraction.Longitude
            });
    }
}