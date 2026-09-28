using HotelBooking.Application.AvailableRooms.Dtos;
using HotelBooking.Application.Common;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Application.Rooms.Dtos;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Repositories;

public class RoomRepository : IRoomRepository
{
    private readonly HotelBookingDbContext _dbContext;
    public RoomRepository(HotelBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<RoomResponseDto>> GetRoomsAsync(RoomFilterDto filter)
    {
        var query = _dbContext.Rooms.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var normalizedSearch = filter.Search.Trim();
            query = query.Where(room => room.RoomNumber.Contains(normalizedSearch));
        }

        if (filter.HotelId.HasValue)
        {
            query = query.Where(room => room.HotelId == filter.HotelId.Value);
        }

        if (filter.RoomType.HasValue)
        {
            query = query.Where(room => room.RoomType == filter.RoomType.Value);
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(room => room.IsActive == filter.IsActive.Value);
        }

        if (filter.IsOperationallyAvailable.HasValue)
        {
            query = query.Where(room => room.IsOperationallyAvailable == filter.IsOperationallyAvailable.Value);
        }

        const int pageSize = 10;
        var items = await query
            .OrderBy(room => room.RoomId)
            .Skip((filter.PageNumber - 1) * pageSize)
            .Take(pageSize + 1)
            .Select(room => new RoomResponseDto
            {
                RoomId = room.RoomId,
                HotelId = room.HotelId,
                HotelName = room.Hotel!.Name,
                RoomNumber = room.RoomNumber,
                RoomType = room.RoomType,
                AdultsCapacity = room.AdultsCapacity,
                ChildCapacity = room.ChildCapacity,
                PricePerNight = room.PricePerNight,
                IsOperationallyAvailable = room.IsOperationallyAvailable,
                IsActive = room.IsActive,
                Description = room.Description,
                CreatedAt = room.CreatedAt,
                UpdatedAt = room.UpdatedAt,
                Images = room.RoomImages
                    .OrderBy(image => image.DisplayOrder)
                    .Select(image => new RoomImageResponseDto
                    {
                        ImageUrl = image.ImageUrl,
                        DisplayOrder = image.DisplayOrder
                    })
                    .ToList()
            })
            .ToListAsync();

        return new PagedResult<RoomResponseDto>
        {
            Items = items.Take(pageSize),
            PageNumber = filter.PageNumber,
            HasNextPage = items.Count > pageSize
        };
    }

    public void Add(Room room)
    {
        _dbContext.Rooms.Add(room);
    }

    public Task<Room?> GetRoomByIdAsync(int id)
    {
        var room = _dbContext.Rooms.FirstOrDefaultAsync(r => r.RoomId == id);
        return room;
    }
    
    public async Task SaveChangesAsync()
    {
        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new ConflictException("A room with the same number already exists in this hotel.");
        }
    }
    
    public Task<bool> ExistsAsync(string roomNumber, int hotelId, int? excludedRoomId = null)
    {
        return _dbContext.Rooms.AsNoTracking().AnyAsync(room =>
            room.RoomNumber == roomNumber &&
            room.HotelId == hotelId &&
            (!excludedRoomId.HasValue || room.RoomId != excludedRoomId.Value));
    }
}
