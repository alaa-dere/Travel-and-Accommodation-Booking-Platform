using HotelBooking.Application.Exceptions;
using HotelBooking.Application.HotelDetails.Dtos;
using HotelBooking.Application.Common;
using HotelBooking.Application.Hotels.Dtos;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Repositories;

public class HotelRepository : IHotelRepository
{
    private readonly HotelBookingDbContext _dbContext;
    public HotelRepository(HotelBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<HotelResponseDto>> GetHotelsAsync(HotelListRequestDto request)
    {
        var query = _dbContext.Hotels.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var normalizedSearch = request.Search.Trim();
            query = query.Where(hotel =>
                hotel.Name.Contains(normalizedSearch) ||
                hotel.OwnerName.Contains(normalizedSearch) ||
                hotel.City.Name.Contains(normalizedSearch));
        }

        const int pageSize = 10;
        var items = await SelectHotelResponse(query)
            .OrderBy(hotel => hotel.HotelId)
            .Skip((request.PageNumber - 1) * pageSize)
            .Take(pageSize + 1)
            .ToListAsync();

        return new PagedResult<HotelResponseDto>
        {
            Items = items.Take(pageSize),
            PageNumber = request.PageNumber,
            HasNextPage = items.Count > pageSize
        };
    }

    public Task<HotelResponseDto?> GetHotelResponseAsync(int hotelId)
    {
        return SelectHotelResponse(
                _dbContext.Hotels.AsNoTracking().Where(hotel => hotel.HotelId == hotelId))
            .FirstOrDefaultAsync();
    }

    public void Add(Hotel hotel)
    {
        _dbContext.Hotels.Add(hotel);
    }

    public Task<Hotel?> GetHotelByIdAsync(int id)
    {
        var hotel = _dbContext.Hotels.FirstOrDefaultAsync(h => h.HotelId == id);
        return hotel;
    }
    
    public async Task SaveChangesAsync()
    {
        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new ConflictException("A hotel with the same name, city, and address already exists.");
        }
    }
    
    public Task<HotelDetailsResponseDto?> GetHotelDetailsAsync(int id)
    {
       return _dbContext.Hotels.AsNoTracking()
           .Where(hotel => hotel.IsActive && hotel.HotelId == id)
           .Select(hotel => new HotelDetailsResponseDto
            {
                HotelId = hotel.HotelId,
                Name = hotel.Name,
                Description = hotel.Description,
                History = hotel.History,
                HotelType = hotel.HotelType,
                Address = hotel.Address,
                Latitude = hotel.Latitude,
                Longitude = hotel.Longitude,
                City = hotel.City.Name,
                Amenities = hotel.HotelAmenities.Select(amenity => new AmenityResponseDto
                {
                    Name = amenity.Amenity.Name,
                    Description = amenity.Amenity.Description
                }).ToList(),
                
                Rating = hotel.Rooms
                    .SelectMany(room => room.Bookings)
                    .Where(booking => booking.BookingStatus == BookingStatus.Completed && booking.Review != null)
                    .Average(booking => (int?)booking.Review!.Rating)
            }).FirstOrDefaultAsync();
    }

    public Task<bool> IsActiveHotelAsync(int hotelId)
    {
        return _dbContext.Hotels.AnyAsync(hotel => hotel.HotelId == hotelId && hotel.IsActive);
    }

    public Task<bool> ExistsAsync(
        string name,
        int cityId,
        string address,
        int? excludedHotelId = null)
    {
        return _dbContext.Hotels.AsNoTracking().AnyAsync(hotel =>
            hotel.Name == name &&
            hotel.CityId == cityId &&
            hotel.Address == address &&
            (!excludedHotelId.HasValue || hotel.HotelId != excludedHotelId.Value));
    }

    private static IQueryable<HotelResponseDto> SelectHotelResponse(IQueryable<Hotel> hotels)
    {
        return hotels.Select(hotel => new HotelResponseDto
        {
            HotelId = hotel.HotelId,
            CityId = hotel.CityId,
            CityName = hotel.City.Name,
            Name = hotel.Name,
            OwnerName = hotel.OwnerName,
            Address = hotel.Address,
            Latitude = hotel.Latitude,
            Longitude = hotel.Longitude,
            HotelType = hotel.HotelType,
            Description = hotel.Description,
            History = hotel.History,
            IsActive = hotel.IsActive,
            Rating = hotel.Rooms
                .SelectMany(room => room.Bookings)
                .Where(booking =>
                    booking.BookingStatus == BookingStatus.Completed &&
                    booking.Review != null)
                .Average(booking => (double?)booking.Review!.Rating),
            RoomsCount = hotel.Rooms.Count,
            CreatedAt = hotel.CreatedAt,
            UpdatedAt = hotel.UpdatedAt
        });
    }
}
