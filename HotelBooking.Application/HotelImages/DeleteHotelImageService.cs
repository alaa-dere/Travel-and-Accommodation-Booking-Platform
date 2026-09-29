using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;

namespace HotelBooking.Application.HotelImages;

public class DeleteHotelImageService : IDeleteHotelImageService
{
    private readonly IHotelImageRepository _images;

    public DeleteHotelImageService(IHotelImageRepository images)
    {
        _images = images;
    }

    public async Task DeleteAsync(int hotelId, int imageId)
    {
        var image = await _images.GetByIdAsync(imageId);
        if (image == null || image.HotelId != hotelId)
        {
            throw new NotFoundException("Hotel image not found.");
        }

        _images.Delete(image);
        await _images.SaveChangesAsync();
    }
}
