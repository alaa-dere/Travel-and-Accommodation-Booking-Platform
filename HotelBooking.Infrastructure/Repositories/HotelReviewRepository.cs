using HotelBooking.Application.HotelReviews.Dtos;
using HotelBooking.Application.Interfaces;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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
        return _dbContext.Reviews.AsNoTracking()
            .Where(review => review.Booking != null && review.Booking.Room != null && review.Booking.Room.HotelId == hotelId && review.Booking.BookingStatus == BookingStatus.Completed)
            .OrderByDescending(review => review.CreatedAt)
            .Select(review => new ReviewResponseDto
            {
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt
            })
           .ToListAsync();
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
        return _dbContext.SaveChangesAsync();
    }
}