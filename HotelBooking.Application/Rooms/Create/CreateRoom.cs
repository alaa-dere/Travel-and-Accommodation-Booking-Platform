using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Rooms.Dtos;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Rooms.Create;

public class CreateRoom : ICreateRoomService
{
    private readonly IHotelRepository _hotelRepository;
    private readonly IRoomRepository  _roomRepository;
    public CreateRoom(IRoomRepository roomRepository,  IHotelRepository hotelRepository)
    {
        _roomRepository = roomRepository;
        _hotelRepository = hotelRepository;
    }
    
    public async Task<RoomResponseDto> CreateRoomAsync(RoomRequestDto request)
    {
        var hotel = await _hotelRepository.GetHotelByIdAsync(request.HotelId);
        if (hotel == null)
        {
            throw new NotFoundException("Hotel doesn't exist");
        }
        
        var room = new Room(
            request.RoomNumber,
            request.RoomType,
            request.PricePerNight,
            request.AdultsCapacity,
            request.ChildCapacity,
            request.HotelId,
            request.Description);

        if (await _roomRepository.ExistsAsync(room.RoomNumber, room.HotelId))
        {
            throw new ConflictException("A room with the same number already exists in this hotel.");
        }

        _roomRepository.Add(room);
        await _roomRepository.SaveChangesAsync();
        
        return new RoomResponseDto
        {
            RoomId = room.RoomId,
            HotelId =  room.HotelId, 
            HotelName = hotel.Name,
            RoomNumber = room.RoomNumber,  
            RoomType = room.RoomType,  
            AdultsCapacity = room.AdultsCapacity, 
            ChildCapacity =  room.ChildCapacity, 
            PricePerNight = room.PricePerNight, 
            IsOperationallyAvailable = room.IsOperationallyAvailable, 
            IsActive = room.IsActive,
            Description = room.Description,
            CreatedAt = room.CreatedAt,
            UpdatedAt = room.UpdatedAt
        };
    }
}
