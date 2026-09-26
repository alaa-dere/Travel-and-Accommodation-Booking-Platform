using HotelBooking.Application.AvailableRooms.Dtos;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;

namespace HotelBooking.Application.AvailableRooms;

public class GetAvailableRooms : IGetAvailableRoomsService
{
    private readonly IHotelRepository _hotelRepository;
    private readonly IAvailableRoomRepository _availableRoomRepository;

    public GetAvailableRooms(IHotelRepository hotelRepository, IAvailableRoomRepository availableRoomRepository)
    {
        _hotelRepository = hotelRepository;
        _availableRoomRepository = availableRoomRepository;
    }

    public async Task<List<AvailableRoomResponseDto>> GetAvailableRoomsAsync(int hotelId, AvailableRoomsRequestDto request)
    {
        if (request.CheckIn == default || request.CheckOut == default)
        {
            throw new BadRequestException("Check-in and check-out dates are required.");
        }

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

        var isActive = await _hotelRepository.IsActiveHotelAsync(hotelId);

        if (!isActive)
        {
            throw new NotFoundException("Hotel not found.");
        }

        return await _availableRoomRepository.GetAvailableRoomsAsync(
            hotelId,
            request.CheckIn,
            request.CheckOut,
            request.Adults,
            request.Children);
    }
}
