using HotelBooking.Application.Exceptions;
using HotelBooking.Application.HotelImages.Dtos;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.HotelImages;

public class AddHotelImageService : IAddHotelImageService
{
    private readonly IHotelRepository _hotels;
    private readonly IHotelImageRepository _images;

    public AddHotelImageService(IHotelRepository hotels, IHotelImageRepository images)
    {
        _hotels = hotels;
        _images = images;
    }

    public async Task AddAsync(int hotelId, AddHotelImageRequestDto request)
    {
        if (await _hotels.GetHotelByIdAsync(hotelId) == null)
        {
            throw new NotFoundException("Hotel not found.");
        }

        var imageUrl = request.ImageUrl.Trim();
        if (await _images.ExistsAsync(hotelId, imageUrl, request.DisplayOrder))
        {
            throw new ConflictException("The image URL or display order already exists for this hotel.");
        }

        await _images.AddAsync(new HotelImage(imageUrl, request.DisplayOrder, hotelId));
        await _images.SaveChangesAsync();
    }
}
