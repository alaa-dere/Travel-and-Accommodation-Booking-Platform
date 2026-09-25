using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;

namespace HotelBooking.Application.NearbyAttractions.Update;

public class UpdateNearbyAttractionService : IUpdateNearbyAttractionService
{
    private readonly INearbyAttractionRepository _attractionRepository;

    public UpdateNearbyAttractionService(INearbyAttractionRepository attractionRepository)
    {
        _attractionRepository = attractionRepository;
    }

    public async Task UpdateAsync(int attractionId, UpdateNearbyAttractionRequestDto request)
    {
        if (attractionId <= 0)
        {
            throw new BadRequestException("Invalid attraction ID.");
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

        var attraction = await _attractionRepository.GetByIdAsync(attractionId);

        if (attraction == null)
        {
            throw new NotFoundException("Nearby attraction not found.");
        }

        attraction.Update(request.Name, request.Description, request.Latitude, request.Longitude);
        await _attractionRepository.SaveChangesAsync();
    }
}