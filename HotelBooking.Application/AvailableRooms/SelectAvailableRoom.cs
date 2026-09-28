using HotelBooking.Application.AvailableRooms.Dtos;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;

namespace HotelBooking.Application.AvailableRooms;

public class SelectAvailableRoom : ISelectAvailableRoomService
{
    private readonly IAvailableRoomRepository _availableRoomRepository;
    private readonly TimeProvider _timeProvider;

    public SelectAvailableRoom(IAvailableRoomRepository availableRoomRepository, TimeProvider timeProvider)
    {
        _availableRoomRepository = availableRoomRepository;
        _timeProvider = timeProvider;
    }

    public async Task<SelectedRoomResponseDto> SelectRoomAsync(int hotelId, int roomId, AvailableRoomsRequestDto request)
    {
        AvailableRoomsRequestValidator.Validate(request, _timeProvider.GetUtcNow().UtcDateTime);

        var criteria = new RoomAvailabilityCriteria(request.CheckIn, request.CheckOut, request.Adults, request.Children);

        var room = await _availableRoomRepository.GetAvailableRoomAsync(hotelId, roomId, criteria);
        if (room == null)
        {
            throw new BadRequestException("Room is not available for the requested stay.");
        }
        var numberOfNights = (request.CheckOut.Date - request.CheckIn.Date).Days;
        var totalPrice = numberOfNights * room.PricePerNight;

        return new SelectedRoomResponseDto
        {
            RoomId = room.RoomId,
            RoomType = room.RoomType,
            CheckIn = request.CheckIn,
            CheckOut = request.CheckOut,
            Adults = request.Adults,
            Children = request.Children,
            PricePerNight = room.PricePerNight,
            TotalPrice = totalPrice
        };
    }
}
