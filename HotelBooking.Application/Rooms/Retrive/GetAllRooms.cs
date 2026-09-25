using HotelBooking.Application.AvailableRooms.Dtos;
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

    public async Task<IEnumerable<RoomResponseDto>> GetAllRoomsAsync(RoomFilterDto filter)
    {
        var rooms = await _roomRepository.GetRoomsAsync(filter);
        var results = rooms.Select(room => new RoomResponseDto()
        {
            RoomId = room.RoomId,
            HotelId = room.HotelId,
            RoomNumber =  room.RoomNumber,
            RoomType = room.RoomType,
            AdultsCapacity = room.AdultsCapacity,
            ChildCapacity = room.ChildCapacity,
            PricePerNight = room.PricePerNight,
            IsOperationallyAvailable = room.IsOperationallyAvailable,
            IsActive = room.IsActive,
            Description = room.Description,
            Images = room.RoomImages
                .OrderBy(image => image.DisplayOrder)
                .Select(image => new RoomImageResponseDto
                {
                    ImageUrl = image.ImageUrl,
                    DisplayOrder = image.DisplayOrder
                })
                .ToList()
        });
        return results;
    }
}