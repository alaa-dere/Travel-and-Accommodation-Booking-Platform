namespace HotelBooking.Application.Cities.Delete;

public interface IDeleteCityService
{
    Task DeleteCityAsync(int cityId);
}