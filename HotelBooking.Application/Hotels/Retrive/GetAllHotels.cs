using HotelBooking.Application.Hotels.Dtos;
using HotelBooking.Application.Interfaces;

namespace HotelBooking.Application.Hotels.Retrive;

public class GetAllHotels : IGetAllHotelsService
{
    private readonly IHotelRepository _hotelRepository;

    public GetAllHotels(IHotelRepository hotelRepository)
    {
        _hotelRepository = hotelRepository;
    }

    public async Task<IEnumerable<HotelResponseDto>> GetAllHotelsAsync(string? search)
    {
        var hotels = await _hotelRepository.GetHotelsAsync(search);
        var results = hotels.Select(hotel => new HotelResponseDto()
        {
            HotelId = hotel.HotelId,
            CityId =  hotel.CityId,
            Name = hotel.Name,
            OwnerName = hotel.OwnerName,
            Address = hotel.Address,
            Latitude =  hotel.Latitude,
            Longitude =  hotel.Longitude,
            HotelType = hotel.HotelType
        });
        return results;
    }
}