using HotelBooking.Application.NearbyAttractions.Dtos;

namespace HotelBooking.Application.NearbyAttractions.GetByHotel;

public interface IGetNearbyAttractionsService
{
    Task<IEnumerable<NearbyAttractionResponseDto>> GetByHotelIdAsync(int hotelId);
}