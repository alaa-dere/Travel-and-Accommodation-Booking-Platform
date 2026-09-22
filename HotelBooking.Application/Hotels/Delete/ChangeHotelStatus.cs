using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;

namespace HotelBooking.Application.Hotels.Delete;

public class ChangeHotelStatus : IChangeHotelStatusService
{
    private readonly IHotelRepository _hotelRepository;
    public ChangeHotelStatus(IHotelRepository  hotelRepository)
    {
        _hotelRepository = hotelRepository;
    }
    public async Task ChangeHotelStatusAsync(int hotelId, bool isActive)
    {
        var hotel = await _hotelRepository.GetHotelByIdAsync(hotelId);
        if (hotel == null)
        {
            throw new NotFoundException("Hotel not found");
        }

        hotel.ChangeStatus(isActive);
        await _hotelRepository.SaveChangesAsync();
    }

}