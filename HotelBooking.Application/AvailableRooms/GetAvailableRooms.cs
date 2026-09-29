using HotelBooking.Application.AvailableRooms.Dtos;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;

namespace HotelBooking.Application.AvailableRooms;

public class GetAvailableRooms : IGetAvailableRoomsService
{
    private readonly IHotelRepository _hotelRepository;
    private readonly IAvailableRoomRepository _availableRoomRepository;
    private readonly TimeProvider _timeProvider;

    public GetAvailableRooms(IHotelRepository hotelRepository, IAvailableRoomRepository availableRoomRepository, TimeProvider timeProvider)
    {
        _hotelRepository = hotelRepository;
        _availableRoomRepository = availableRoomRepository;
        _timeProvider = timeProvider;
    }

    public async Task<List<AvailableRoomResponseDto>> GetAvailableRoomsAsync(int hotelId, AvailableRoomsRequestDto request)
    {
        AvailableRoomsRequestValidator.Validate(request, _timeProvider.GetUtcNow().UtcDateTime);

        var isActive = await _hotelRepository.IsActiveHotelAsync(hotelId);

        if (!isActive)
        {
            throw new NotFoundException("Hotel not found.");
        }

        var criteria = new RoomAvailabilityCriteria(request.CheckIn, request.CheckOut, request.Adults, request.Children);
        return await _availableRoomRepository.GetAvailableRoomsAsync(hotelId, criteria);
    }
}
