namespace HotelBooking.Application.NearbyAttractions.Remove;

public interface IRemoveNearbyAttractionService
{
    Task RemoveAsync(int attractionId);
}