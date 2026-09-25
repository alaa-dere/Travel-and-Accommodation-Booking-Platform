using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.NearbyAttractions.Dtos;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.NearbyAttractions.Create;

public class CreateNearbyAttractionService : ICreateNearbyAttractionService
{
    private readonly INearbyAttractionRepository _attractionRepository;
    private readonly IHotelRepository _hotelRepository;

    public CreateNearbyAttractionService(INearbyAttractionRepository attractionRepository, IHotelRepository hotelRepository)
    {
        _attractionRepository = attractionRepository;
        _hotelRepository = hotelRepository;
    }

    public async Task CreateAsync(CreateNearbyAttractionRequestDto request)
    {
        if (request.HotelId <= 0)
        {
            throw new BadRequestException("Invalid hotel ID.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new BadRequestException("Attraction name is required.");
        }

        if (request.Name.Length > 100)
        {
            throw new BadRequestException("Attraction name cannot exceed 100 characters.");
        }

        if (request.Description?.Length > 500)
        {
            throw new BadRequestException("Attraction description cannot exceed 500 characters.");
        }

        if (request.Latitude < -90 || request.Latitude > 90)
        {
            throw new BadRequestException("Latitude must be between -90 and 90.");
        }

        if (request.Longitude < -180 || request.Longitude > 180)
        {
            throw new BadRequestException("Longitude must be between -180 and 180.");
        }

        var hotel = await _hotelRepository.GetHotelByIdAsync(request.HotelId);

        if (hotel == null)
        {
            throw new NotFoundException("Hotel not found.");
        }

        var attraction = new NearbyAttraction(request.HotelId, request.Name, request.Description, request.Latitude, request.Longitude);

        await _attractionRepository.AddAsync(attraction);
        await _attractionRepository.SaveChangesAsync();
    }
}