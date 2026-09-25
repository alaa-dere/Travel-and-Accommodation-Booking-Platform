using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;

namespace HotelBooking.Application.NearbyAttractions.Remove;

public class RemoveNearbyAttractionService : IRemoveNearbyAttractionService
{
    private readonly INearbyAttractionRepository _attractionRepository;

    public RemoveNearbyAttractionService(INearbyAttractionRepository attractionRepository)
    {
        _attractionRepository = attractionRepository;
    }

    public async Task RemoveAsync(int attractionId)
    {
        if (attractionId <= 0)
        {
            throw new BadRequestException("Invalid attraction ID.");
        }

        var attraction = await _attractionRepository.GetByIdAsync(attractionId);

        if (attraction == null)
        {
            throw new NotFoundException("Nearby attraction not found.");
        }

        _attractionRepository.Remove(attraction);
        await _attractionRepository.SaveChangesAsync();
    }
}