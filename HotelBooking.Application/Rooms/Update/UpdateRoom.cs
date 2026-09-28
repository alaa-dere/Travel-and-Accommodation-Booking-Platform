using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Rooms.Dtos;

namespace HotelBooking.Application.Rooms.Update;

public class UpdateRoom : IUpdateRoomService
{
    private readonly IRoomRepository _roomRepository;
    private readonly IHotelRepository _hotelRepository;

    public UpdateRoom(IRoomRepository roomRepository,  IHotelRepository hotelRepository)
    {
        _roomRepository = roomRepository;
        _hotelRepository = hotelRepository;
    }
    
    public async Task<RoomResponseDto> UpdateRoomAsync(int id, RoomRequestDto request)
    {
        var room = await _roomRepository.GetRoomByIdAsync(id);
        if (room == null)
        {
            throw new NotFoundException("Room doesn't exist");
        }
        
        var hotel = await _hotelRepository.GetHotelByIdAsync(request.HotelId);
        if (hotel == null)
        {
            throw new NotFoundException("Hotel does not exist");
        }

        var normalizedRoomNumber = request.RoomNumber.Trim();
        if (await _roomRepository.ExistsAsync(normalizedRoomNumber, request.HotelId, id))
        {
            throw new ConflictException("A room with the same number already exists in this hotel.");
        }

        room.Update(
            normalizedRoomNumber,
            request.RoomType,
            request.PricePerNight,
            request.AdultsCapacity,
            request.ChildCapacity,
            request.HotelId,
            request.Description);
        await _roomRepository.SaveChangesAsync();
        
        return new RoomResponseDto
        {
            RoomId = room.RoomId,
            HotelId = room.HotelId, 
            HotelName = hotel.Name,
            RoomNumber = room.RoomNumber,
            RoomType =  room.RoomType,
            AdultsCapacity = room.AdultsCapacity,
            ChildCapacity = room.ChildCapacity,
            PricePerNight = room.PricePerNight,
            IsOperationallyAvailable = room.IsOperationallyAvailable,
            IsActive = room.IsActive,
            Description = room.Description,
            CreatedAt = room.CreatedAt,
            UpdatedAt = room.UpdatedAt
        };
    }
}
