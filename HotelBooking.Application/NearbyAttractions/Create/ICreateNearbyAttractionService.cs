using HotelBooking.Application.NearbyAttractions.Dtos;

namespace HotelBooking.Application.NearbyAttractions.Create;

public interface ICreateNearbyAttractionService
{
    Task CreateAsync(CreateNearbyAttractionRequestDto request);
}