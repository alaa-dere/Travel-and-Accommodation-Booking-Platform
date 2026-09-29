namespace HotelBooking.Application.NearbyAttractions.Update;

public interface IUpdateNearbyAttractionService
{
    Task UpdateAsync(int attractionId, UpdateNearbyAttractionRequestDto request);
}