using HotelBooking.Application.AvailableRooms.Dtos;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;

namespace HotelBooking.Application.AvailableRooms;

public class SelectAvailableRoom : ISelectAvailableRoomService
{
    private readonly IAvailableRoomRepository _availableRoomRepository;

    public SelectAvailableRoom(IAvailableRoomRepository availableRoomRepository)
    {
        _availableRoomRepository = availableRoomRepository;
    }

    public async Task<SelectedRoomResponseDto> SelectRoomAsync(int hotelId, int roomId, AvailableRoomsRequestDto request)
    {
        if (request.CheckOut <= request.CheckIn)
        {
            throw new BadRequestException("Check-out date must be after check-in date.");
        }

        if (request.Adults <= 0)
        {
            throw new BadRequestException("Adults must be greater than zero.");
        }

        if (request.Children < 0)
        {
            throw new BadRequestException("Children cannot be negative.");
        }

        var room = await _availableRoomRepository.GetAvailableRoomAsync(hotelId, roomId, request.CheckIn, request.CheckOut, request.Adults, request.Children);
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