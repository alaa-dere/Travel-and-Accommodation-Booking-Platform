using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;

namespace HotelBooking.Application.Cities.Delete;

public class DeleteCity : IDeleteCityService
{
    private readonly ICityRepository _cityRepository;

    public DeleteCity(ICityRepository cityRepository)
    {
        _cityRepository = cityRepository;
    }
    
    public async Task DeleteCityAsync(int cityId)
    {
        var city = await _cityRepository.GetCityByIdAsync(cityId);
        if (city == null)
        {
            throw new NotFoundException("City doesn't exist");
        }
        
       var anyHotels = await _cityRepository.HasHotelsAsync(cityId);
       if (anyHotels)
       {
        throw new ConflictException("City cannot be deleted because it has associated hotels.");
        
       }
       
       _cityRepository.DeleteCity(city);
       await _cityRepository.SaveChangesAsync();
    }
}