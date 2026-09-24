using HotelBooking.Application;
using HotelBooking.Application.Search;
using HotelBooking.Application.Search.Dtos;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Repositories;

public class HotelSearchRepository : IHotelSearchRepository
{
    private readonly HotelBookingDbContext _dbContext;

    public HotelSearchRepository(HotelBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

   public async Task<IEnumerable<HotelSearchResult>> GetCandidateHotelsAsync(HotelSearchRequestDto request)
{
    var searchTerm = $"%{request.Destination.Trim()}%";

    IQueryable<Hotel> query = _dbContext.Hotels.AsNoTracking()
        .Where(hotel => hotel.IsActive)
        .Where(hotel =>
            EF.Functions.Like(hotel.Name, searchTerm) ||
            EF.Functions.Like(hotel.City.Name, searchTerm))
        .Where(hotel => hotel.Rooms.Count(room =>
            room.IsActive &&
            room.IsOperationallyAvailable &&
            room.AdultsCapacity >= request.Adults &&
            room.ChildCapacity >= request.Children &&
            (!request.MinPrice.HasValue || room.PricePerNight >= request.MinPrice.Value) &&
            (!request.MaxPrice.HasValue || room.PricePerNight <= request.MaxPrice.Value) &&
            (!request.RoomType.HasValue || room.RoomType == request.RoomType.Value) &&
            !room.Bookings.Any(booking =>
                booking.BookingStatus != BookingStatus.Cancelled &&
                booking.CheckIn < request.CheckOut &&
                booking.CheckOut > request.CheckIn)

        ) >= request.Rooms)

        .Include(hotel => hotel.City)
        .Include(hotel => hotel.Rooms.Where(room =>
            room.IsActive && room.IsOperationallyAvailable &&
            room.AdultsCapacity >= request.Adults &&
            room.ChildCapacity >= request.Children &&
            (!request.MinPrice.HasValue || room.PricePerNight >= request.MinPrice.Value) &&
            (!request.MaxPrice.HasValue || room.PricePerNight <= request.MaxPrice.Value) &&
            (!request.RoomType.HasValue || room.RoomType == request.RoomType.Value) &&
            !room.Bookings.Any(booking =>
                booking.BookingStatus != BookingStatus.Cancelled &&
                booking.CheckIn < request.CheckOut &&
                booking.CheckOut > request.CheckIn)
        ));

    if (request.HotelType.HasValue)
    {
        query = query.Where(hotel => hotel.HotelType == request.HotelType.Value);
    }

    if (request.AmenityIds != null && request.AmenityIds.Any())
    {
        query = query.Where(hotel => request.AmenityIds.All(amenityId => hotel.HotelAmenities.Any(hotelAmenity => hotelAmenity.AmenityId == amenityId)));
    }

    if (request.MinRating.HasValue)
    {
        query = query.Where(hotel =>
            hotel.Rooms
                .SelectMany(room => room.Bookings)
                .Any(booking => booking.Review != null) &&
            hotel.Rooms
                .SelectMany(room => room.Bookings)
                .Where(booking => booking.Review != null)
                .Average(booking => booking.Review!.Rating)
            >= request.MinRating.Value);
    }

    return await query.Select(hotel => new HotelSearchResult
    {
        Hotel = hotel,
        Rating = hotel.Rooms
            .SelectMany(room => room.Bookings)
            .Where(booking => booking.Review != null)
            .Average(booking => (double?)booking.Review!.Rating)
    }).ToListAsync();
}
}