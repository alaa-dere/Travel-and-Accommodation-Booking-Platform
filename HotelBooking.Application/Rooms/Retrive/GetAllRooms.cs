using HotelBooking.Application.Common;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Rooms.Dtos;

namespace HotelBooking.Application.Rooms.Retrive;

public class GetAllRooms : IGetAllRoomsService
{
    private readonly IRoomRepository _roomRepository;

    public GetAllRooms(IRoomRepository roomRepository)
    {
        _roomRepository = roomRepository;
    }

    public async Task<PagedResult<RoomResponseDto>> GetAllRoomsAsync(RoomFilterDto filter)
    {
        if (filter.PageNumber < 1)
        {
            throw new BadRequestException("Page number must be greater than zero.");
        }

        return await _roomRepository.GetRoomsAsync(filter);
    }
}
