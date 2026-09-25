using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Interfaces;

public interface INearbyAttractionRepository
{
    Task AddAsync(NearbyAttraction attraction);
    Task<IEnumerable<NearbyAttraction>> GetByHotelIdAsync(int hotelId);
    Task<NearbyAttraction?> GetByIdAsync(int attractionId);
    void Remove(NearbyAttraction attraction);
    Task SaveChangesAsync();
}