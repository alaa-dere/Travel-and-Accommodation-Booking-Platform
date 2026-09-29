using HotelBooking.Application;
using HotelBooking.Application.Common;
using HotelBooking.Application.Search;
using HotelBooking.Application.Search.Dtos;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Repositories;

public class HotelSearchRepository : IHotelSearchRepository
{
    private readonly HotelBookingDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public HotelSearchRepository(HotelBookingDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<PagedResult<HotelSearchResult>> GetCandidateHotelsAsync(HotelSearchRequestDto request)
    {
        var searchTerm = $"%{request.Destination.Trim()}%";
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

        var eligibleRooms = _dbContext.Rooms.AsNoTracking()
            .Where(room =>
                room.IsActive && room.IsOperationallyAvailable &&
                (!request.MinPrice.HasValue || room.PricePerNight >= request.MinPrice.Value) &&
                (!request.MaxPrice.HasValue || room.PricePerNight <= request.MaxPrice.Value) &&
                (!request.RoomType.HasValue || room.RoomType == request.RoomType.Value) &&
                !room.Bookings.Any(booking =>
                    booking.BookingStatus != BookingStatus.Cancelled &&
                    (booking.BookingStatus != BookingStatus.Pending || booking.PendingExpiresAt > utcNow) &&
                    booking.CheckIn < request.CheckOut &&
                    booking.CheckOut > request.CheckIn));

        var hotelsWithEnoughCapacity = eligibleRooms
            .GroupBy(room => room.HotelId)
            .Where(rooms => rooms.Count() >= request.Rooms)
            .Where(rooms => rooms
                .OrderByDescending(room => room.AdultsCapacity)
                .Take(request.Rooms)
                .Sum(room => room.AdultsCapacity) >= request.Adults)
            .Where(rooms => rooms
                .OrderByDescending(room => room.ChildCapacity)
                .Take(request.Rooms)
                .Sum(room => room.ChildCapacity) >= request.Children)
            .Select(rooms => rooms.Key);

        IQueryable<Hotel> query = _dbContext.Hotels.AsNoTracking()
            .Where(hotel => hotel.IsActive)
            .Where(hotel =>
                EF.Functions.Like(hotel.Name, searchTerm) ||
                EF.Functions.Like(hotel.City.Name, searchTerm))
            .Where(hotel => hotelsWithEnoughCapacity.Contains(hotel.HotelId))
            .Include(hotel => hotel.City);

        if (request.HotelType.HasValue)
        {
            query = query.Where(hotel => hotel.HotelType == request.HotelType.Value);
        }

        if (request.AmenityIds is { Count: > 0 })
        {
            query = query.Where(hotel => request.AmenityIds.All(amenityId =>
                hotel.HotelAmenities.Any(hotelAmenity => hotelAmenity.AmenityId == amenityId)));
        }

        if (request.MinRating.HasValue)
        {
            query = query.Where(hotel =>
                hotel.Rooms
                    .SelectMany(room => room.Bookings)
                    .Any(booking => booking.BookingStatus == BookingStatus.Completed && booking.Review != null) &&

                hotel.Rooms
                    .SelectMany(room => room.Bookings)
                    .Where(booking => booking.BookingStatus == BookingStatus.Completed && booking.Review != null)
                    .Average(booking => booking.Review!.Rating) >= request.MinRating.Value);
        }

        const int pageSize = 10;
        var skip = (request.PageNumber - 1) * pageSize;
        var results = await query
            .OrderBy(hotel => hotel.HotelId)
            .Skip(skip)
            .Take(pageSize + 1)
            .Select(hotel => new HotelSearchResult
            {
                Hotel = hotel,
                Rating = hotel.Rooms
                    .SelectMany(room => room.Bookings)
                    .Where(booking => booking.BookingStatus == BookingStatus.Completed && booking.Review != null)
                    .Average(booking => (double?)booking.Review!.Rating),
                ThumbnailUrl = hotel.HotelImages
                    .OrderBy(image => image.DisplayOrder)
                    .Select(image => image.ImageUrl)
                    .FirstOrDefault()
            })
            .ToListAsync();

        var hotelIds = results.Select(result => result.Hotel.HotelId).ToList();
        var roomsByHotel = await eligibleRooms
            .Where(room => hotelIds.Contains(room.HotelId))
            .ToListAsync();

        foreach (var result in results)
        {
            result.Hotel.Rooms = roomsByHotel
                .Where(room => room.HotelId == result.Hotel.HotelId)
                .ToList();
        }

        var hasNextPage = results.Count > pageSize;
        var items = results.Take(pageSize);

        return new PagedResult<HotelSearchResult>
        {
            Items = items,
            PageNumber = request.PageNumber,
            HasNextPage = hasNextPage
        };
    }
}
