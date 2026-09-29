using HotelBooking.Application.HotelReviews.Dtos;
using HotelBooking.Application.Exceptions;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace HotelBooking.Infrastructure.Repositories;
 
public class HotelReviewRepository : IHotelReviewRepository
{
    private readonly HotelBookingDbContext _dbContext;

    public HotelReviewRepository(HotelBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<ReviewResponseDto>> GetReviewsByHotelIdAsync(int hotelId)
    {
        return GetReviewsByHotelIdAsync(hotelId, 1);
    }

    public Task<List<ReviewResponseDto>> GetReviewsByHotelIdAsync(int hotelId, int pageNumber)
    {
        const int pageSize = 10;
        var query = _dbContext.Reviews.AsNoTracking()
            .Where(review =>
                review.Booking != null && review.Booking.Room != null &&
                review.Booking.Room.HotelId == hotelId &&
                review.Booking.BookingStatus == BookingStatus.Completed);

        return query
            .OrderByDescending(review => review.CreatedAt)
            .ThenByDescending(review => review.ReviewId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize + 1)
            .Select(review => new ReviewResponseDto
            {
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt
            })
            .ToListAsync();
    }

    public Task<double?> GetAverageRatingAsync(int hotelId)
    {
        return _dbContext.Reviews.AsNoTracking()
            .Where(review =>
                review.Booking != null && review.Booking.Room != null &&
                review.Booking.Room.HotelId == hotelId &&
                review.Booking.BookingStatus == BookingStatus.Completed)
            .AverageAsync(review => (double?)review.Rating);
    }
    
    public Task<Booking?> GetBookingForReviewAsync(int bookingId)
    {
        return _dbContext.Bookings
            .Include(booking => booking.Room)
            .Include(booking => booking.Review)
            .FirstOrDefaultAsync(booking => booking.BookingId == bookingId);
    }
    
    public async Task AddReviewAsync(Review review)
    {
        await _dbContext.Reviews.AddAsync(review);
    }
    
    public Task SaveChangesAsync()
    {
        return SaveReviewChangesAsync();
    }

    private async Task SaveReviewChangesAsync()
    {
        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new ConflictException("A review has already been submitted for this booking.");
        }
    }
}
