using HotelBooking.Application.Exceptions;
using HotelBooking.Application.HotelDetails.Dtos;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Repositories;

public class HotelRepository : IHotelRepository
{
    private readonly HotelBookingDbContext _dbContext;
    public HotelRepository(HotelBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<Hotel>> GetHotelsAsync(string? search)
    {
        var searchQuery = _dbContext.Hotels.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            searchQuery = searchQuery.Where(h => h.Name.Contains(search) || h.OwnerName.Contains(search));
        }
        return await searchQuery.ToListAsync();
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
        await _dbContext.SaveChangesAsync();
    }
    
    public void DeleteHotel(Hotel hotel){
        _dbContext.Hotels.Remove(hotel);
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
                
                Rating = hotel.Rooms.SelectMany(room => room.Bookings)
                    .Where(booking => booking.Review != null)
                    .Average(booking => (int?)booking.Review!.Rating)
            }).FirstOrDefaultAsync();
    }

    public Task<bool> IsActiveHotelAsync(int hotelId)
    {
        return _dbContext.Hotels.AnyAsync(hotel => hotel.HotelId == hotelId && hotel.IsActive);
    }
}