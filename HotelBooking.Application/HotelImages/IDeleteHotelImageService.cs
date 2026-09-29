namespace HotelBooking.Application.HotelImages;

public interface IDeleteHotelImageService
{
    Task DeleteAsync(int hotelId, int imageId);
}
